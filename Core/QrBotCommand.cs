using QrBot.Architecture;

namespace QrBot.Core;

public class QrBotCommand : Command
{
    public QrBotCommand(string key, HandlerAction action) : base(key, action)
    {
        
    }

    public override string GetDescription(string? code)
    {
        return QrBotStrings.GetLocalizedString(Description, code);
    }
}