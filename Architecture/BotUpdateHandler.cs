using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace QrBot.Architecture
{
    public delegate Task HandlerAction(ITelegramBotClient client, Update update);

    public class BotUpdateHandler : IUpdateHandler
    {
        private readonly Dictionary<string, Command> _commands = new();
        private readonly Dictionary<long, HandlerAction> _pendingRequests = new();
        private readonly Regex _commandRegex;
        private ILogger? _logger { get; }
        protected Bot Bot;

        protected BotCommand[] Commands 
        { 
            get
            {
                var commands = new BotCommand[_commands.Count];
                var i = 0;

                foreach (var key in _commands.Keys)
                {
                    commands[i++] = new BotCommand
                    {
                        Command = key,
                        Description = _commands[key].Description
                    };
                }

                return commands;
            } 
        }

        public BotUpdateHandler(string botUsername, Bot bot, ILogger? logger = null)
        {
            Bot = bot;
            _commandRegex = new(@$"(?<=^/)\w+((?=@{botUsername}$)|$)", RegexOptions.Compiled);
            _logger = logger;
        }

        public virtual async Task HandlePollingErrorAsync(
            ITelegramBotClient botClient, 
            Exception exception, 
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        public async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update update, 
            CancellationToken cancellationToken)
        {
            var action = TryParseCommand(update);

            if (update.Message?.From is { })
            {
                if (action is not null)
                {
                    RemovePendingRequest(update.Message.From.Id);
                    try
                    {
                        await action.Invoke(botClient, update);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, ex.Message);
                    }
                }

                if (_pendingRequests.TryGetValue(update.Message.From.Id, out var request) && action is null)
                {
                    try
                    {
                        await request.Invoke(botClient, update);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, ex.Message);
                    }
                }
            }
        }

        private HandlerAction? TryParseCommand(Update update)
        {
            if (update.Type != UpdateType.Message || update.Message is not { } message) return null;

            var match = _commandRegex.Match(message.Text?.ToLower() ?? "");

            if (match.Success)
            {
                var command = match.Value;

                if (_commands.TryGetValue(command, out var action))
                {
                    return action.Handler;
                }
            }

            return null;
        }

        protected void DefineCommand(string command, string description, HandlerAction action)
        {
            if (_commands.ContainsKey(command))
            {
                throw new BotUpdateHandlerException("The specified command is already added");
            }

            _commands.Add(command.ToLower(), new Command(description, action));
        }

        protected void AddPendingRequest(long fromId, HandlerAction action)
        {
            _pendingRequests.Remove(fromId);
            _pendingRequests.Add(fromId, action);           
        }

        protected void RemovePendingRequest(long fromId)
        {
            _pendingRequests.Remove(fromId);
        }
    }
}
