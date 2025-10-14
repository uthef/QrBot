using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace QrBot.Architecture
{
    public delegate Task HandlerAction(ITelegramBotClient client, Update update, string? data = null);

    public class BotUpdateHandler : IUpdateHandler
    {
        private readonly Dictionary<string, Command> _commands = new();
        private readonly ConcurrentDictionary<long, HandlerAction> _pendingRequests = new();
        private readonly Regex _commandRegex;
        
        private ILogger? Logger { get; }
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
            Logger = logger;
        }

        public virtual async Task HandlePollingErrorAsync(
            ITelegramBotClient botClient, 
            Exception exception, 
            CancellationToken cancellationToken)
        {
            Logger?.LogError(exception, null);
            await Task.CompletedTask;
        }

        protected virtual async Task OnCallbackQueryAsync(ITelegramBotClient botClient, Update update, HandlerAction? pendingHandler,
            CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
        }
        
        public async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update update, 
            CancellationToken cancellationToken)
        {
            if (update is { Type: UpdateType.CallbackQuery, CallbackQuery.Message.From: not null, CallbackQuery.Data: not null })
            {
                _pendingRequests.TryGetValue(update.CallbackQuery.From.Id, out var handler);
                await OnCallbackQueryAsync(botClient, update, handler, cancellationToken);
                
                return;
            }
            
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
                        Logger?.LogError(ex, ex.Message);
                    }

                    return;
                }

                if (_pendingRequests.TryGetValue(update.Message.From.Id, out var request))
                {
                    try
                    {
                        await request.Invoke(botClient, update);
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogError(ex, ex.Message);
                    }

                    return;
                }

                await HandleInvalidCommandAsync(botClient, update);
            }
        }

        protected virtual async Task HandleInvalidCommandAsync(ITelegramBotClient botClient, Update update)
        {
            await Task.CompletedTask;
        }

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source,
            CancellationToken cancellationToken)
        {
            Logger?.LogError(exception, null);
            await Task.CompletedTask;
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
            _pendingRequests.Remove(fromId, out var _);
            _pendingRequests[fromId] = action;
        }

        protected void RemovePendingRequest(long fromId)
        {
            _pendingRequests.TryRemove(fromId, out var _);
        }
    }
}
