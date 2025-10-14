using System.Collections.Immutable;

namespace QrBot.Core;

public static class QrBotStrings
{
    private const int EnglishLangIndex = 0;
    private const int RussianLangIndex = 1;

    public const string InvalidQrCodeImage = nameof(InvalidQrCodeImage);
    public const string TextTooLong = nameof(TextTooLong);
    public const string TextMessageExpected = nameof(TextMessageExpected);
    public const string UnableToDecode = nameof(UnableToDecode);
    
    public const string DecodedText = nameof(DecodedText);
    public const string SelectedColor = nameof(SelectedColor);
    public const string ImageCaption = nameof(ImageCaption);
    
    public const string ScanRequest = nameof(ScanRequest);
    public const string QrCodeDataRequest = nameof(QrCodeDataRequest);
    public const string ColorSchemeRequest = nameof(ColorSchemeRequest);
    
    public const string AvailableCommandsInfo = nameof(AvailableCommandsInfo);
    public const string StartCommandDescription = nameof(StartCommandDescription);
    public const string GenerateCommandDescription = nameof(GenerateCommandDescription);
    public const string ScanCommandDescription = nameof(ScanCommandDescription);

    public static int GetLangIndex(string? code)
    {
        return code switch
        {
            "ru" => RussianLangIndex,
            _ => EnglishLangIndex
        };
    }

    public static string GetLocalizedString(string key, string? code)
    {
        return Map[key][GetLangIndex(code)];
    }
    
    public static readonly ImmutableDictionary<string, string[]> Map =
        new Dictionary<string, string[]>
        {
            { 
                InvalidQrCodeImage, 
                [
                    "A single *compressed* image is expected. Try again",
                    "Принимается только сжатое изображение. Попробуйте снова."
                ] 
            },
            {
                TextTooLong, 
                [
                    "The text length must not exceed 512 characters. Send /gen_qr to try again",
                    "Длина текста не должна превышать 512 символов. Отправьте /gen_qr, чтобы попробовать снова"
                ]
            },
            {
                TextMessageExpected,
                [
                    "A text message is expected. Try again",
                    "Принимается только текстовое сообщение. Попробуйте ещё раз"
                ]
            },
            {
                ScanRequest,
                [
                    "Send me an image containing QR code or barcode",
                    "Отправьте изображение с QR-кодом или штрих-кодом"
                ]
            },
            {
                QrCodeDataRequest,
                [
                    "Enter QR code data",
                    "Введите текст"
                ]
            },
            {
                ColorSchemeRequest,
                [
                    "Select color scheme",
                    "Выберите цветовую схему"
                ]
            },
            {
                AvailableCommandsInfo,
                [
                    "Here is the list of all available commands",
                    "Вот список всех доступных команд"
                ]
            },
            {
                UnableToDecode,
                [
                    "Sorry, I'm unable to decode this image. Send /scan to try another one",
                    "Извините, считать данные с этого изображения не получилоось. Отправьте /scan, чтобы попробовать отсканировать другое"
                ]
            },
            {
                StartCommandDescription,
                [
                    "List available commands",
                    "Вывести список доступных команд"
                ]
            },
            {
                GenerateCommandDescription,
                [
                    "Generate a new QR code image",
                    "Сгенерировать новый QR-код"
                ]
            },
            {
                ScanCommandDescription,
                [
                    "Scan QR code or barcode image",
                    "Считать данные с QR-кода или штрих-кода"
                ]
            },
            {
                DecodedText,
                [
                    "Decoded text\n\n{0}",
                    "Раскодированный текст\n\n{0}"
                ]
            },
            {
                SelectedColor,
                [
                    "Selected color: *{0}*",
                    "Выбранный цвет: *{0}*"
                ]
            },
            {
                QrColor.WhiteOnBlack,
                [
                    "White on black",
                    "Белый на чёрном"
                ]
            },
            {
                QrColor.BlackOnWhite,
                [
                    "Black on white",
                    "Чёрный на белом"
                ]
            },
            {
                QrColor.Red,
                [
                    "Red",
                    "Красный"
                ]
            },
            {
                QrColor.Blue,
                [
                    "Blue",
                    "Синий"
                ]
            },
            {
                QrColor.Green,
                [
                    "Green",
                    "Зелёный"
                ]
            },
            {
                QrColor.Yellow,
                [
                    "Yellow",
                    "Жёлтый"
                ]
            },
            {
                QrColor.Pink,
                [
                    "Pink",
                    "Розовый"
                ]
            },
            {
                QrColor.Purple,
                [
                    "Purple",
                    "Фиолетовый"
                ]
            },
            {
                ImageCaption,
                [
                    "Your QR code is ready!",
                    "Ваш QR-код готов!"
                ]
            }
        }.ToImmutableDictionary();
}