using QrBot.Architecture;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using ZXing.QrCode;
using ZXing;
using System.Runtime.InteropServices;
using SkiaSharp;
using ZXing.SkiaSharp;
using System.Text;

namespace QrBot
{
    public class QrBotHandler : BotUpdateHandler
    {
        public const int DefaultImageSize = 512,
            DefaultMargin = 1;

        public QrBotHandler(string botUsername, Bot bot) : base(botUsername, bot)
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

                await client.SendTextMessageAsync(update.Message.Chat.Id, 
                    strBuilder.ToString(), 
                    replyToMessageId: update.Message.MessageId);
            });

            DefineCommand("gen_qr", "Generate a new QR code image", GenerateCommandHandler);
            DefineCommand("scan", "Scan QR code or barcode image", ScanCommandHandler);

            bot.Client.SetMyCommandsAsync(Commands).Wait();
        }

        public override Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            throw exception;
        }

        private async Task GenerateCommandHandler(ITelegramBotClient client, Update update)
        {
            if (update.Type is UpdateType.Message && update.Message is { } && update.Message.From is { })
            {
                AddPendingRequest(update.Message.From.Id, GenerateQrCode);

                await client.SendTextMessageAsync(update.Message.Chat.Id,
                    "Enter QR code data",
                    replyToMessageId: update.Message.MessageId);
            }
        }

        private async Task ScanCommandHandler(ITelegramBotClient client, Update update)
        {
            if (update.Type is UpdateType.Message && update.Message is { } && update.Message.From is { })
            {
                AddPendingRequest(update.Message.From.Id, ScanBarcode);

                await client.SendTextMessageAsync(update.Message.Chat.Id, 
                    "Send me an image containing QR code or barcode", 
                    replyToMessageId: update.Message.MessageId);
            }
        }

        private async Task ScanBarcode(ITelegramBotClient client, Update update)
        {
            if (update.Type is UpdateType.Message && update.Message is { } && update.Message.From is { } && update.Message.Photo is {})
            {
                RemovePendingRequest(update.Message.From.Id);

                BarcodeReader reader = new();

                var photo = update.Message.Photo.Last();
                var file = await client.GetFileAsync(photo.FileId);
                using var stream = new MemoryStream();

                if (file.FilePath is null) return;

                await client.DownloadFileAsync(file.FilePath, stream);

                stream.Seek(0, SeekOrigin.Begin);

                var barcodeBitmap = SKBitmap.Decode(stream);
                var result = reader.Decode(barcodeBitmap);

                await client.SendTextMessageAsync(
                    update.Message.Chat.Id,
                    result is null ? "Sorry, I'm unable to decode this image" : $"Decoded text\n\n{result.Text}",
                    replyToMessageId: update.Message.MessageId);
            }
        }

        private async Task GenerateQrCode(ITelegramBotClient client, Update update)
        {
            if (update.Type is UpdateType.Message && update.Message is { } && update.Message.From is { })
            {
                RemovePendingRequest(update.Message.From.Id);

                int width = DefaultImageSize, height = DefaultImageSize, margin = DefaultMargin;
                string data = update.Message.Text ?? "";

                if (data.Length > 512)
                {
                    await client.SendTextMessageAsync(update.Message.Chat.Id,
                        "The text length must not exceed 512 characters", 
                        replyToMessageId: update.Message.MessageId);
                    return;
                }

                var encodingOptions = new QrCodeEncodingOptions
                {
                    Height = height,
                    Width = width,
                    Margin = margin
                };

                encodingOptions.Hints.Add(EncodeHintType.CHARACTER_SET, "UTF-8");

                var qrCodeWriter = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = encodingOptions
                };

                var pixelData = qrCodeWriter.Write(data);

                var gcHandle = GCHandle.Alloc(pixelData.Pixels, GCHandleType.Pinned);
                var info = new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
                var bitmap = new SKBitmap(pixelData.Width, pixelData.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);

                bitmap.InstallPixels(info, gcHandle.AddrOfPinnedObject(), info.RowBytes);

                var stream = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Jpeg, 100).AsStream();

#pragma warning disable CS8604 // Possible null reference argument.
                await client.SendPhotoAsync(update.Message.Chat.Id,
                    stream, 
                    replyToMessageId: update.Message.MessageId);
#pragma warning restore CS8604 // Possible null reference argument.
            }
        }
    }
}
