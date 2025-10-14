using System.Diagnostics.CodeAnalysis;
using Telegram.Bot.Types.ReplyMarkups;

namespace QrBot.Core;

public static class ColorSelectionMarkup
{
    private static InlineKeyboardMarkup? _markup = null;
    public static InlineKeyboardMarkup Get(string? langCode)
    {
        if (_markup is not null) return _markup;

        _markup = new InlineKeyboardMarkup();
        
        _markup.AddButtons(
            new(QrBotStrings.GetLocalizedString(QrColor.BlackOnWhite, langCode))
            {
                CallbackData = QrColor.BlackOnWhite
            }, 
            new(QrBotStrings.GetLocalizedString(QrColor.WhiteOnBlack, langCode))
            {
                CallbackData = QrColor.WhiteOnBlack 
            });

        _markup.AddNewRow();
        
        _markup.AddButtons(
            new(QrBotStrings.GetLocalizedString(QrColor.Red, langCode))
            {
                CallbackData = QrColor.Red 
            },
            new(QrBotStrings.GetLocalizedString(QrColor.Green, langCode))
            {
                CallbackData = QrColor.Green 
            },
            new(QrBotStrings.GetLocalizedString(QrColor.Blue, langCode))
            {
                CallbackData = QrColor.Blue 
            });

        _markup.AddNewRow();

        _markup.AddButtons(
            new(QrBotStrings.GetLocalizedString(QrColor.Yellow, langCode))
            {
                CallbackData = QrColor.Yellow
            },
            new(QrBotStrings.GetLocalizedString(QrColor.Pink, langCode))
            {
                CallbackData = QrColor.Pink
            },
            new(QrBotStrings.GetLocalizedString(QrColor.Purple, langCode))
            {
                CallbackData = QrColor.Purple
            });

        return _markup;
    }
}