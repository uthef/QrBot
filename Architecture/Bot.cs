using Telegram.Bot;
using Telegram.Bot.Polling;

namespace QrBot.Architecture
{
    public class Bot
    {
        public TelegramBotClient Client { get; }
        public IUpdateHandler UpdateHandler { get; }

        internal string Token { get; }

        public Bot(string token, Type handlerType)
        {
            Token = token;
            Client = new TelegramBotClient(token);

            string? username = Client.GetMeAsync().GetAwaiter().GetResult().Username;

            if (!typeof(BotUpdateHandler).IsAssignableFrom(handlerType))
            {
                throw new BotUpdateHandlerException($"Handler must be type of {typeof(BotUpdateHandler)}");
            }

            UpdateHandler = Activator.CreateInstance(handlerType, username, this) as BotUpdateHandler 
                ?? throw new BotUpdateHandlerException($"Cannot create instance of type {handlerType}");
        }
    }
}
