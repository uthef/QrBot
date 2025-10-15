using System.Diagnostics.CodeAnalysis;
using Telegram.Bot.Types.ReplyMarkups;

namespace QrBot.Core;

public static class ColorSelectionMarkup
{
    public static InlineKeyboardMarkup Create(string? langCode)
    {
        var markup = new InlineKeyboardMarkup();
        
        markup.AddButtons(
            new(QrBotStrings.GetLocalizedString(QrColor.BlackOnWhite, langCode))
            {
                CallbackData = QrColor.BlackOnWhite
            }, 
            new(QrBotStrings.GetLocalizedString(QrColor.WhiteOnBlack, langCode))
            {
                CallbackData = QrColor.WhiteOnBlack 
            });

        markup.AddNewRow();
        
        markup.AddButtons(
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

        markup.AddNewRow();

        markup.AddButtons(
            new(QrBotStrings.GetLocalizedString(QrColor.Yellow, langCode))
            {
                CallbackData = QrColor.Yellow
            },
            new(QrBotStrings.GetLocalizedString(QrColor.Orange, langCode))
            {
                CallbackData = QrColor.Orange
            },
            new(QrBotStrings.GetLocalizedString(QrColor.Purple, langCode))
            {
                CallbackData = QrColor.Purple
            });

        return markup;
    }
}