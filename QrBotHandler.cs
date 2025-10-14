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

namespace QrBot
{
    public class QrBotHandler : BotUpdateHandler
    {
        public const int DefaultImageSize = 512,
            DefaultMargin = 1;

        public QrBotHandler(string botUsername, Bot bot, ILogger logger) : base(botUsername, bot, logger)
        {
 
            DefineCommand("start", "List available commands", async (client, update) =>
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
            });

            DefineCommand("gen_qr", "Generate a new QR code image", GenerateCommandHandler);
            DefineCommand("scan", "Scan QR code or barcode image", ScanCommandHandler);

            bot.Client.SetMyCommands(Commands).Wait();
        }

        public override Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(exception.Message + exception.StackTrace);
            return Task.CompletedTask;
        }

        private async Task GenerateCommandHandler(ITelegramBotClient client, Update update)
        {
            if (update.Type is UpdateType.Message && update.Message is { } && update.Message.From is { })
            {
                AddPendingRequest(update.Message.From.Id, GenerateBarcode);

                await client.SendMessage(update.Message.Chat.Id,
                    "Enter QR code data",
                    replyParameters: new()
                    {
                        MessageId = update.Message.MessageId
                    });
            }
        }

        private async Task ScanCommandHandler(ITelegramBotClient client, Update update)
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

        private async Task ScanBarcode(ITelegramBotClient client, Update update)
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

        private async Task GenerateBarcode(ITelegramBotClient client, Update update)
        {
            if (update.Type is not UpdateType.Message || update.Message is not { }) return;

            if  (update.Message.Text is { } && update.Message.From is { })
            {
                RemovePendingRequest(update.Message.From.Id);

                int width = DefaultImageSize, height = DefaultImageSize, margin = DefaultMargin;
                string data = update.Message.Text ?? "";

                if (new StringInfo(data).LengthInTextElements > 512)
                {
                    await client.SendMessage(update.Message.Chat.Id,
                        "The text length must not exceed 512 characters. Send /gen_qr to try again",
                        replyParameters: new()
                        {
                            MessageId = update.Message.MessageId
                        });
                    
                    return;
                }

                var qrCodeWriter = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Height = height,
                        Width = width,
                        Margin = margin,
                        CharacterSet = "UTF-8"
                    }
                };

                using var bitmap = qrCodeWriter.Write(data);
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

            await client.SendMessage(update.Message.Chat.Id,
                "A text message is expected. Try again",
                replyParameters: new()
                {
                    MessageId = update.Message.MessageId
                });
        }
    }
}
