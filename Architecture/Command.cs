namespace QrBot.Architecture
{
    public class Command
    {
        public HandlerAction Handler { get; }
        public string Description { get; }

        public Command(string description, HandlerAction handler)
        {
            Description = description;
            Handler = handler;
        }

    }
}
