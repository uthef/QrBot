using QrBot.Architecture;
using Telegram.Bot;
using QrBot;
using Telegram.Bot.Polling;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var botFactory = new BotFactory();
var botTokens = config.GetSection("Bots").Get<Dictionary<string, string>>();
var baseUrl = config.GetSection("BaseUrl").Get<string>();

if (botTokens is { } && botTokens.TryGetValue("QrBot", out string? token))
{
    var updateHandlerLogger = new Logger<BotUpdateHandler>(LoggerFactory.Create(options =>
    {
        options.AddConsole();
    }));

    var bot = new Bot(token, typeof(QrBotHandler), updateHandlerLogger);
    await bot.Client.DeleteWebhookAsync();

    if (builder.Environment.IsDevelopment())
    {
        bot.Client.StartReceiving(bot.UpdateHandler, new ReceiverOptions
        {
            ThrowPendingUpdates = true
        });
    }
    else
    {
        var url = $"{baseUrl}/Bot/Post/{token}";
        await bot.Client.SetWebhookAsync(url);
    }

    botFactory.Register(bot);
}

builder.Services.AddSingleton(botFactory);
builder.Services.AddControllersWithViews().AddNewtonsoftJson();

var app = builder.Build();
app.MapControllers();
app.Run();
