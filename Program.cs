using QrBot.Architecture;
using Telegram.Bot;
using QrBot.Core;
using Telegram.Bot.Polling;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var logger = new Logger<Program>(LoggerFactory.Create(options => options.AddConsole()));

var botFactory = new BotFactory();
var botTokens = config.GetSection("Bots").Get<Dictionary<string, string>>();
var baseUrl = config.GetSection("BaseUrl").Get<string>();

if (botTokens is null || !botTokens.TryGetValue("QrBot", out var token))
{
    throw new Exception("Missing QrBot configuration");
}

var updateHandlerLogger = new Logger<BotUpdateHandler>(
    LoggerFactory.Create(options => options.AddConsole()));

var bot = new Bot(token, typeof(QrBotHandler), updateHandlerLogger);
botFactory.Register(bot);
await bot.Client.DeleteWebhook();

if (builder.Environment.IsDevelopment())
{
    logger.LogInformation("Started in long polling mode");
    bot.Client.StartReceiving(bot.UpdateHandler, new ReceiverOptions
    {
        DropPendingUpdates = true,
    });
    
    await Task.Delay(Timeout.Infinite);
}
else
{
    logger.LogInformation("Started in webhook mode");
    logger.LogInformation($"Base url: {baseUrl}");
        
    var url = $"{baseUrl}/Bot/Post/{token}";
    await bot.Client.SetWebhook(url);
    
    builder.Services.AddSingleton(botFactory);
    builder.Services.AddControllers();
    
    var app = builder.Build();
    
    app.MapControllers();
    app.Run();
}