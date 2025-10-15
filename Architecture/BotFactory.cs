namespace QrBot.Architecture
{
    public class BotFactory
    {
        private Dictionary<string, Bot> Bots { get; } = new();
        
        public Bot? Get(string botToken) => Bots.TryGetValue(botToken, out Bot? bot) ? bot : null;
        public void Register(Bot bot)
        {
            if (Bots.ContainsKey(bot.Token)) 
                throw new BotFactoryException("The bot token is already registered");

            Bots.Add(bot.Token, bot);
        }

        public bool Unregister(Bot bot) => Bots.Remove(bot.Token);
        
    }
}
