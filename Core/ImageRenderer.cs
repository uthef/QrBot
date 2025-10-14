using SkiaSharp;
using ZXing;
using ZXing.QrCode;
using ZXing.SkiaSharp;
using ZXing.SkiaSharp.Rendering;

namespace QrBot.Core;

public class ImageRenderer
{
    public static void GenerateQrCode(MemoryStream ms, string text, string qrColor, int width, int height, int margin)
    {
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
            Renderer = GetRendererByColorName(qrColor)
        };

        using var bitmap = qrCodeWriter.Write(text);
        using var skImage = SKImage.FromBitmap(bitmap);
        using var skData = skImage.Encode();
        
        skData.SaveTo(ms);
        ms.Position = 0;
    }

    private static SKBitmapRenderer GetRendererByColorName(string qrColor) => qrColor switch
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
    
}