using QrBot.Architecture;
using Telegram.Bot;
using QrBot;
using Telegram.Bot.Polling;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var botFactory = new BotFactory();
var botTokens = config.GetSection("Bots").Get<Dictionary<string, string>>();
var baseUrl = config.GetSection("BaseUrl").Get<string>();

builder.Services.AddSingleton(botFactory);
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (botTokens is { } && botTokens.TryGetValue("QrBot", out string? token))
{
    var updateHandlerLogger = new Logger<BotUpdateHandler>(LoggerFactory.Create(options =>
    {
        options.AddConsole();
    }));

    var bot = new Bot(token, typeof(QrBotHandler), updateHandlerLogger);
    await bot.Client.DeleteWebhook();

    if (builder.Environment.IsDevelopment())
    {
        app.Logger.LogInformation("Started in long polling mode");
        bot.Client.StartReceiving(bot.UpdateHandler, new ReceiverOptions
        {
            DropPendingUpdates = true,
        });
    }
    else
    {
        app.Logger.LogInformation("Started in webhook mode");
        app.Logger.LogInformation($"Base url: {baseUrl}");
        
        var url = $"{baseUrl}/Bot/Post/{token}";
        await bot.Client.SetWebhook(url);
    }

    botFactory.Register(bot);
}


app.MapControllers();
app.Run();
