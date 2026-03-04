using FinTrack.AI.Services;
using FinTrack.Interfaces;
using FinTrack.Providers;
using FinTrack.Services;
using FinTrack.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// ??? Load configuration ???
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// ??? Validate configuration ???
var environment = config["SemanticKernel:Environment"] ?? "Development";
var provider = config["SemanticKernel:Provider"] ?? "Ollama";
var endpoint = config["SemanticKernel:Endpoint"];
var apiKey = config["SemanticKernel:ApiKey"];
var modelId = config["SemanticKernel:ModelId"] ?? "llama3.1";

if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(apiKey))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERROR: SemanticKernel:ApiKey is required for OpenAI provider.");
    Console.ResetColor();
    return;
}

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine($"Env      : {environment}");

if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase) ||
    environment.Equals("Local", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine($"Provider : Ollama (local) {(endpoint != null ? $"({endpoint})" : "")}");
    Console.WriteLine($"Model    : {modelId}");
}
else if (provider.Equals("Multi", StringComparison.OrdinalIgnoreCase))
{
    var models = config.GetSection("SemanticKernel:OpenRouter:Models").Get<string[]>() ?? [];
    Console.WriteLine($"Provider : Multi (OpenRouter ? {models.Length} models + local fallback)");
    foreach (var m in models)
        Console.WriteLine($"           • {m}");
    var localFallback = config.GetValue<bool>("SemanticKernel:LocalFallback", true);
    if (localFallback)
        Console.WriteLine($"           • {modelId} (Ollama local fallback)");
}
else
{
    Console.WriteLine($"Provider : {provider} {(endpoint != null ? $"({endpoint})" : "")}");
    Console.WriteLine($"Model    : {modelId}");
}

Console.ResetColor();

// ??? Wire up services (same as the API, but without ASP.NET) ???
// Resolve AppData folder — try multiple paths to handle different run contexts
var baseDir = AppContext.BaseDirectory; // bin\Debug\net10.0
var solutionDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
var dataFolder = Path.Combine(solutionDir, "FinTrack", "AppData");

if (!Directory.Exists(dataFolder))
{
    // Fallback: running from project directory (dotnet run)
    dataFolder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "FinTrack", "AppData"));
}

if (!Directory.Exists(dataFolder))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERROR: AppData folder not found. Searched:");
    var path1 = Path.Combine(solutionDir, "FinTrack", "AppData");
    var path2 = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "FinTrack", "AppData"));
    Console.WriteLine($"  {path1}");
    Console.WriteLine($"  {path2}");
    Console.ResetColor();
    return;
}

Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine($"Data     : {dataFolder}");
Console.ResetColor();

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

// ??? Create ChatService ???
var chatService = new ChatService(assetService, valuationService, priceProvider, config);

// ??? Verify data is readable ???
var existingAssets = await assetService.GetAssetsAsync("default-user");
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine($"Assets   : {existingAssets.Count} loaded from {dataFolder}");
Console.ResetColor();

// ??? Console chat loop ???
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine();
Console.WriteLine("????????????????????????????????????????????");
Console.WriteLine("?       FinTrack AI — Console Chat         ?");
Console.WriteLine("????????????????????????????????????????????");
Console.WriteLine("?  Ask about your gold, silver, FDs, or    ?");
Console.WriteLine("?  net worth. Type 'quit' to exit,         ?");
Console.WriteLine("?  'reset' to clear chat history.          ?");
Console.WriteLine("????????????????????????????????????????????");
Console.ResetColor();
Console.WriteLine();

while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("You > ");
    Console.ResetColor();

    var input = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(input)) continue;

    if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Goodbye!");
        Console.ResetColor();
        break;
    }

    if (input.Equals("reset", StringComparison.OrdinalIgnoreCase))
    {
        chatService.ResetChat();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Chat history cleared.");
        Console.ResetColor();
        Console.WriteLine();
        continue;
    }

    try
    {
        var result = await chatService.ChatAsync(input);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("AI > ");
        Console.ResetColor();
        Console.WriteLine(result.Reply);

        if (result.DataChanged)
        {
            // Verify the JSON was actually updated by re-reading
            var currentAssets = await assetService.GetAssetsAsync("default-user");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  [?? Assets modified — {currentAssets.Count} assets now in JSON]");
            Console.ResetColor();
        }

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {ex.Message}");
        Console.ResetColor();
        Console.WriteLine();
    }
}
