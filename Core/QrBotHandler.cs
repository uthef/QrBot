using System.Globalization;
using QrBot.Architecture;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using SkiaSharp;
using ZXing.SkiaSharp;
using System.Text;

namespace QrBot.Core;
public class QrBotHandler : BotUpdateHandler
{
    public const int DefaultImageSize = 512, DefaultMargin = 1;

    public QrBotHandler(string botUsername, Bot bot, ILogger logger) : base(botUsername, bot, logger)
    {
        DefineCommand("start", new QrBotCommand(QrBotStrings.StartCommandDescription, ListCommandsHandler));
        DefineCommand("gen_qr", new QrBotCommand(QrBotStrings.GenerateCommandDescription, GenerateCommandHandler));
        DefineCommand("scan", new QrBotCommand(QrBotStrings.ScanCommandDescription, ScanCommandHandler));

        bot.Client.SetMyCommands(GetCommands("en")).Wait();
        bot.Client.SetMyCommands(GetCommands("ru"), languageCode: "ru").Wait();
    }

    private async Task ListCommandsHandler(ITelegramBotClient client, Update update, string? data = null)
    {
        if (update.Message is null) return;

        var strBuilder = new StringBuilder();
        strBuilder.AppendLine(QrBotStrings.GetLocalizedString(QrBotStrings.AvailableCommandsInfo, 
            update.Message.From?.LanguageCode));
        strBuilder.AppendLine();

        foreach (var item in GetCommands(update.Message.From?.LanguageCode))
        {
            strBuilder.AppendLine($"/{item.Command} - {item.Description}");
        }
        await client.SendMessage(update.Message.Chat.Id, 
            strBuilder.ToString(), 
            replyParameters: new()
            {
                MessageId = update.Message.MessageId
            });
    }
    
    public override Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, 
        CancellationToken cancellationToken)
    {
        Console.WriteLine(exception.Message + exception.StackTrace);
        return Task.CompletedTask;
    }
    
    private async Task GenerateCommandHandler(ITelegramBotClient client, Update update, string? data)
    {
        if (update is not { Type: UpdateType.Message, Message.From: not null } )
        {
            return;
        }
        
        AddPendingRequest(update.Message.From.Id, GetQrCodeData);
        
        var langCode = update.Message.From.LanguageCode;
        var replyMarkup = ColorSelectionMarkup.Get(langCode);
        
        await client.SendMessage(update.Message.Chat.Id,
            QrBotStrings.GetLocalizedString(QrBotStrings.ColorSchemeRequest, update.Message.From.LanguageCode), 
            replyMarkup: replyMarkup);
    }

    private async Task GetQrCodeData(ITelegramBotClient client, Update update, string? data)
    {
        if (data is not null && update is { Message: not null })
        {
            AddPendingRequest(update.Message.Chat.Id, 
                (x, y, _) => GenerateBarcode(x, y, data));

            await client.SendMessage(update.Message.Chat.Id,
                QrBotStrings.GetLocalizedString(QrBotStrings.QrCodeDataRequest, update.Message.From?.LanguageCode),
                replyParameters: new()
                {
                    MessageId = update.Message.MessageId
                });
        }
    }

    private async Task ScanCommandHandler(ITelegramBotClient client, Update update, string? data)
    {
        if (update.Type is UpdateType.Message && update.Message is { } && update.Message.From is { })
        {
            AddPendingRequest(update.Message.From.Id, ScanBarcode);

            await client.SendMessage(update.Message.Chat.Id, 
                QrBotStrings.GetLocalizedString(QrBotStrings.ScanRequest, update.Message.From.LanguageCode), 
                replyParameters: new()
                {
                    MessageId = update.Message.MessageId
                });
        }
    }

    private async Task ScanBarcode(ITelegramBotClient client, Update update, string? data)
    {
        if (update.Type is not UpdateType.Message || update.Message is not { }) return;
        
        if (update.Message.From is { } && update.Message.Photo is { })
        {
            RemovePendingRequest(update.Message.From.Id);

            BarcodeReader reader = new();
            reader.Options.TryInverted = true;
            reader.Options.TryHarder = true;

            var photo = update.Message.Photo.Last();
            var file = await client.GetFile(photo.FileId);
            using var stream = new MemoryStream();

            if (file.FilePath is null) return;

            await client.DownloadFile(file.FilePath, stream);

            stream.Seek(0, SeekOrigin.Begin);

            var barcodeBitmap = SKBitmap.Decode(stream);

            var result = reader.Decode(barcodeBitmap);
            var langCode = update.Message.From.LanguageCode;

            string replyText;

            if (result is null)
            {
                replyText = QrBotStrings.GetLocalizedString(QrBotStrings.UnableToDecode, langCode);
            }
            else
            {
                replyText = string.Format(QrBotStrings.GetLocalizedString(QrBotStrings.DecodedText, langCode),
                    result.Text);
            }
            
            await client.SendMessage(
                update.Message.Chat.Id,
                replyText,
                replyParameters: new()
                {
                    MessageId = update.Message.MessageId
                });
            
            return;
        }

        await client.SendMessage(
            update.Message.Chat.Id,
            QrBotStrings.GetLocalizedString(QrBotStrings.InvalidQrCodeImage, update.Message.From?.LanguageCode),
            parseMode: ParseMode.Markdown,
            replyParameters: new()
            {
                MessageId = update.Message.MessageId
            });
        
    }

    protected override async Task HandleInvalidCommandAsync(ITelegramBotClient botClient, Update update)
    {
        await ListCommandsHandler(botClient, update);
    }

    protected override async Task OnCallbackQueryAsync(ITelegramBotClient botClient, Update update, 
        HandlerAction? pendingHandler, CancellationToken cancellationToken = default)
    {
        if (update is { CallbackQuery: null } or { CallbackQuery.Message: null } or { CallbackQuery.Data: null })
        {
            return;
        }

        if (update.CallbackQuery.Message.Text is not null && update.CallbackQuery.Data.StartsWith(QrColor.Prefix))
        {
            var langCode = update.CallbackQuery.From.LanguageCode;
            await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: cancellationToken);

            await botClient.EditMessageReplyMarkup(update.CallbackQuery.Message.Chat.Id,
                update.CallbackQuery.Message.Id,
                replyMarkup: null, cancellationToken: cancellationToken);

            await botClient.EditMessageText(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.Id,
                string.Format(
                    QrBotStrings.GetLocalizedString(QrBotStrings.SelectedColor, langCode), 
                    QrBotStrings.GetLocalizedString(update.CallbackQuery.Data ?? QrColor.WhiteOnBlack, langCode).ToLower()),
                ParseMode.Markdown,
                cancellationToken: cancellationToken);
            
            update.Message = update.CallbackQuery.Message;
            update.Message.From = update.CallbackQuery.From;
            
            await GetQrCodeData(botClient, update, update.CallbackQuery.Data);
        }
    }

    private async Task GenerateBarcode(ITelegramBotClient client, Update update, string? data)
    {
        if (data is not null && update is { Type: UpdateType.Message, Message: { Text: not null, From: not null } })
        {
            RemovePendingRequest(update.Message.From.Id);

            int width = DefaultImageSize, height = DefaultImageSize, margin = DefaultMargin;
            string qrData = update.Message.Text ?? "";

            if (new StringInfo(qrData).LengthInTextElements > 512)
            {
                await client.SendMessage(update.Message.Chat.Id,
                    QrBotStrings.GetLocalizedString(QrBotStrings.TextTooLong, update.Message.From.LanguageCode),
                    replyParameters: new()
                    {
                        MessageId = update.Message.MessageId
                    });
                
                return;
            }
            
            using var stream = new MemoryStream();
            ImageRenderer.GenerateQrCode(stream, qrData, data, width, height, margin);
            
            await client.SendPhoto(update.Message.Chat.Id,
                stream,
                replyParameters: new()
                {
                    MessageId = update.Message.MessageId
                },
                caption: QrBotStrings.GetLocalizedString(QrBotStrings.ImageCaption, update.Message.From.LanguageCode));

            return;
        }

        if (update.Message is null) return;
        
        await client.SendMessage(update.Message.Chat.Id,
            QrBotStrings.GetLocalizedString(QrBotStrings.TextMessageExpected, update.Message.From?.LanguageCode),
            replyParameters: new()
            {
                MessageId = update.Message.MessageId
            });
    }
}