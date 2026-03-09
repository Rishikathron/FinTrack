using FinTrack.AI.Services;
using FinTrack.Interfaces;
using FinTrack.Providers;
using FinTrack.Services;
using FinTrack.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

// ??? Load configuration ???
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// ??? Validate Telegram token ???
var botToken = config["Telegram:BotToken"];
if (string.IsNullOrWhiteSpace(botToken) || botToken == "YOUR_TELEGRAM_BOT_TOKEN")
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERROR: Set your Telegram bot token in appsettings.json ? Telegram:BotToken");
    Console.WriteLine("       Get one from @BotFather on Telegram: https://t.me/BotFather");
    Console.ResetColor();
    return;
}

// Optional: restrict to specific Telegram user IDs (empty = allow everyone)
var allowedUserIds = config.GetSection("Telegram:AllowedUserIds").Get<long[]>() ?? [];

// ??? Resolve AppData folder (same as Console) ???
// ??? Resolve AppData folder ???
// In Docker: /app/AppData (volume-mounted)
// In dev: relative to solution directory
var dataFolder = Path.Combine(AppContext.BaseDirectory, "AppData");

if (!Directory.Exists(dataFolder))
{
    var baseDir = AppContext.BaseDirectory;
    var solutionDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
    dataFolder = Path.Combine(solutionDir, "FinTrack", "AppData");
}

if (!Directory.Exists(dataFolder))
    dataFolder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "FinTrack", "AppData"));

if (!Directory.Exists(dataFolder))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERROR: AppData folder not found.");
    Console.ResetColor();
    return;
}

// ??? Wire up services (same services as Console & API — shared FinTrack.AI ChatService) ???
var repository = new JsonFileRepository(dataFolder);
IAssetService assetService = new AssetService(repository);

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("User-Agent",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

var cache = new MemoryCache(new MemoryCacheOptions());
var priceLogger = loggerFactory.CreateLogger<MetalPriceProvider>();
IPriceProvider priceProvider = new MetalPriceProvider(httpClient, cache, config, priceLogger);
IValuationService valuationService = new ValuationService(assetService, priceProvider);

// ??? Per-user ChatService (each Telegram user gets their own chat history) ???
// Uses the SAME ChatService class from FinTrack.AI — same plugins, same fallback logic
var chatServices = new Dictionary<long, ChatService>();

ChatService GetOrCreateChat(long userId)
{
    if (!chatServices.TryGetValue(userId, out var chat))
    {
        chat = new ChatService(assetService, valuationService, priceProvider, config);
        chatServices[userId] = chat;
    }
    return chat;
}

// ??? Create Telegram bot ???
var botClient = new TelegramBotClient(botToken);
using var cts = new CancellationTokenSource();

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandleErrorAsync,
    receiverOptions: new ReceiverOptions { AllowedUpdates = [UpdateType.Message] },
    cancellationToken: cts.Token
);

var me = await botClient.GetMe(cts.Token);
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine($"????????????????????????????????????????????");
Console.WriteLine($"?       FinTrack AI — Telegram Bot         ?");
Console.WriteLine($"????????????????????????????????????????????");
Console.WriteLine($"?  Bot      : @{me.Username,-27}?");
Console.WriteLine($"?  Data     : ...{dataFolder[^30..]}?");
Console.WriteLine($"?  Users    : {(allowedUserIds.Length == 0 ? "Everyone" : $"{allowedUserIds.Length} allowed"),-28}?");
Console.WriteLine($"????????????????????????????????????????????");
Console.ResetColor();
Console.WriteLine("Press Ctrl+C to stop...\n");

// Keep running until Ctrl+C
var tcs = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); tcs.SetResult(); };
await tcs.Task;

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("Bot stopped.");
Console.ResetColor();

// ??? Telegram update handler ???

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
{
    if (update.Message is not { Text: { } messageText } message)
        return;

    var userId = message.From!.Id;
    var chatId = message.Chat.Id;

    // Access control
    if (allowedUserIds.Length > 0 && !allowedUserIds.Contains(userId))
    {
        await bot.SendMessage(chatId, "? You're not authorized to use this bot.", cancellationToken: ct);
        return;
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write($"[{message.From.FirstName}] ");
    Console.ResetColor();
    Console.WriteLine(messageText);

    // ??? /start ???
    if (messageText.Equals("/start", StringComparison.OrdinalIgnoreCase))
    {
        await bot.SendMessage(chatId,
            "?? Welcome to FinTrack AI!\n\n" +
            "I can manage your gold, silver, and fixed deposit portfolio.\n\n" +
            "Try:\n" +
            "• \"What's my net worth?\"\n" +
            "• \"Add 8g of gold at ?7500/g on Jan 15\"\n" +
            "• \"Show my assets\"\n" +
            "• \"What can you do?\"\n\n" +
            "Type /reset to clear chat history.",
            cancellationToken: ct);
        return;
    }

    // ??? /reset ???
    if (messageText.Equals("/reset", StringComparison.OrdinalIgnoreCase))
    {
        var chat = GetOrCreateChat(userId);
        chat.ResetChat();
        await bot.SendMessage(chatId, "?? Chat history cleared. Start fresh!", cancellationToken: ct);
        return;
    }

    // Show typing indicator while AI processes
    await bot.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);

    try
    {
        // This is the SAME ChatService.ChatAsync() used by Console and the Angular UI
        var chatService = GetOrCreateChat(userId);
        var result = await chatService.ChatAsync(messageText);

        // Send reply (split if > Telegram's 4096 char limit)
        var reply = result.Reply;
        if (reply.Length <= 4096)
        {
            await bot.SendMessage(chatId, reply, cancellationToken: ct);
        }
        else
        {
            for (var i = 0; i < reply.Length; i += 4096)
            {
                var chunk = reply.Substring(i, Math.Min(4096, reply.Length - i));
                await bot.SendMessage(chatId, chunk, cancellationToken: ct);
            }
        }

        if (result.DataChanged)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  [?? Assets modified by {message.From.FirstName}]");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  Error: {ex.Message}");
        Console.ResetColor();
        await bot.SendMessage(chatId, "?? Something went wrong. Please try again.", cancellationToken: ct);
    }
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, HandleErrorSource source, CancellationToken ct)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Telegram error: {exception.Message}");
    Console.ResetColor();
    return Task.CompletedTask;
}
