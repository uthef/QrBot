using System.Globalization;
using QrBot.Architecture;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using ZXing.QrCode;
using ZXing;
using SkiaSharp;
using ZXing.SkiaSharp;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;
using ZXing.SkiaSharp.Rendering;

namespace QrBot
{
    public class QrBotHandler : BotUpdateHandler
    {
        public const int DefaultImageSize = 512,
            DefaultMargin = 1;

        public QrBotHandler(string botUsername, Bot bot, ILogger logger) : base(botUsername, bot, logger)
        {
            DefineCommand("start", "List available commands", ListCommandsHandler);
            DefineCommand("gen_qr", "Generate a new QR code image", GenerateCommandHandler);
            DefineCommand("scan", "Scan QR code or barcode image", ScanCommandHandler);

            bot.Client.SetMyCommands(Commands).Wait();
        }

        private async Task ListCommandsHandler(ITelegramBotClient client, Update update, string? data = null)
        {
            if (update.Message is null) return;

            var strBuilder = new StringBuilder();
            strBuilder.AppendLine("Here is the list of all available commands");
            strBuilder.AppendLine();

            foreach (var item in Commands)
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
        
        public override Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(exception.Message + exception.StackTrace);
            return Task.CompletedTask;
        }
        
        private async Task GenerateCommandHandler(ITelegramBotClient client, Update update, string? data)
        {
            if (update is not { Type: UpdateType.Message, Message.From: not null, } )
            {
                return;
            }
            
            AddPendingRequest(update.Message.From.Id, GetQrCodeData);
            
            var replyMarkup = new InlineKeyboardMarkup();
            
            replyMarkup.AddButtons(
                new(QrColor.BlackOnWhite) { CallbackData = QrColor.BlackOnWhite }, 
                new(QrColor.WhiteOnBlack) { CallbackData = QrColor.WhiteOnBlack });

            replyMarkup.AddNewRow();

            replyMarkup.AddButtons(
                new(QrColor.Red) { CallbackData = QrColor.Red },
                new(QrColor.Green) { CallbackData = QrColor.Green },
                new(QrColor.Blue) { CallbackData = QrColor.Blue });
            
            await client.SendMessage(update.Message.Chat.Id, "Select color scheme", replyMarkup: replyMarkup);
        }

        private async Task GetQrCodeData(ITelegramBotClient client, Update update, string? data)
        {
            if (data is not null && update is { Message: not null })
            {
                AddPendingRequest(update.Message.Chat.Id, 
                    (x, y, _) => GenerateBarcode(x, y, data));

                await client.SendMessage(update.Message.Chat.Id,
                    "Enter QR code data",
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
                    "Send me an image containing QR code or barcode", 
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
                await client.SendMessage(
                    update.Message.Chat.Id,
                    result is null ? "Sorry, I'm unable to decode this image. Send /scan to try another one" : $"Decoded text\n\n{result.Text}",
                    replyParameters: new()
                    {
                        MessageId = update.Message.MessageId
                    });
                return;
            }

            await client.SendMessage(
                update.Message.Chat.Id,
                "A single *compressed* image is expected. Try again",
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
            if (update is { CallbackQuery: null } or { CallbackQuery.Message: null }) return;

            if (update.CallbackQuery.Message.Text is {} msgText && msgText.Contains("color scheme"))
            {

                await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: cancellationToken);

                await botClient.EditMessageReplyMarkup(update.CallbackQuery.Message.Chat.Id,
                    update.CallbackQuery.Message.Id,
                    replyMarkup: null, cancellationToken: cancellationToken);

                await botClient.EditMessageText(update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.Id,
                    $"Selected color: *{update.CallbackQuery.Data?.ToLower()}*", ParseMode.Markdown,
                    cancellationToken: cancellationToken);

                update.Message = update.CallbackQuery.Message;
                
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
                        "The text length must not exceed 512 characters. Send /gen_qr to try again",
                        replyParameters: new()
                        {
                            MessageId = update.Message.MessageId
                        });
                    
                    return;
                }

                SKBitmapRenderer renderer = data switch
                {
                    QrColor.WhiteOnBlack => new()
                    {
                        Foreground = SKColors.White,
                        Background = SKColors.Black
                    },
                    QrColor.Red => new()
                    {
                        Foreground = SKColors.White,
                        Background = SKColors.DarkRed
                    },
                    QrColor.Green => new()
                    {
                        Foreground = SKColors.White,
                        Background = SKColors.DarkGreen
                    },
                    QrColor.Blue => new()
                    {
                        Foreground = SKColors.White,
                        Background = SKColors.DarkBlue
                    },
                    _ => new()
                    {
                        Foreground = SKColors.Black,
                        Background = SKColors.White
                    }
                };

                var qrCodeWriter = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Height = height,
                        Width = width,
                        Margin = margin,
                        CharacterSet = "UTF-8"
                    },
                    Renderer = renderer
                };

                using var bitmap = qrCodeWriter.Write(qrData);
                using var skImage = SKImage.FromBitmap(bitmap);
                using var skData = skImage.Encode();
                using var stream = new MemoryStream();

                skData.SaveTo(stream);

                stream.Position = 0;

#pragma warning disable CS8604 // Possible null reference argument.
                await client.SendPhoto(update.Message.Chat.Id,
                    stream,
                    replyParameters: new()
                    {
                        MessageId = update.Message.MessageId
                    });
#pragma warning restore CS8604 // Possible null reference argument.

                return;
            }

            if (update.Message is null) return;
            
            await client.SendMessage(update.Message.Chat.Id,
                "A text message is expected. Try again",
                replyParameters: new()
                {
                    MessageId = update.Message.MessageId
                });
        }
    }
}
