using FinTrack.AI.Plugins;
using FinTrack.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FinTrack.AI.Services;

public class ChatResult
{
    public string Reply { get; set; } = string.Empty;
    public bool DataChanged { get; set; }
}

public class ChatService
{
    private readonly Kernel _kernel;
    private readonly ChatHistory _chatHistory;
    private readonly AssetsPlugin _assetsPlugin;

    // Ordered list of service IDs to try (for Multi/fallback mode). Empty = single provider.
    private readonly IReadOnlyList<string> _serviceFallbackOrder;

    private const string SystemPrompt = """
        You are FinTrack AI — a helpful personal finance assistant for an Indian user.
        You can manage their gold, silver, and fixed deposit assets.

        Capabilities:
        - View all assets, net worth, profit/loss, and per-asset breakdown
        - Add gold (in grams with purchase rate), silver, or fixed deposits
        - Update or delete any asset
        - Check live gold (22K) and silver prices with daily change
        - Provide bank-wise FD breakdown

        Rules:
        - Always show amounts in INR (?) with Indian formatting.
        - Gold is tracked in grams (22K purity, 8 grams = 1 sovereign).
        - Silver is tracked in grams.
        - FDs use compound interest: A = P × (1 + r/100)^t.
        - Be concise and helpful. Use bullet points for lists.
        - When the user asks about their portfolio, use the tools to fetch actual data — don't guess.
        - Do NOT show placeholders like "[fetching ...]". Either show the final numbers or say clearly what you couldn't fetch.
        - After adding, updating, or deleting an asset, briefly confirm the action with the key details.
        - IMPORTANT: You MUST use the provided tools/functions to perform actions,
          but you must NEVER mention tools, functions, plugins, JSON, or tool calls in your reply.
          Just speak naturally as if you performed the actions yourself.
        - CRITICAL: When the user wants to add an asset, you MUST collect ALL required details BEFORE calling the add function:
          * For Gold/Silver: grams, purchase rate per gram (?), and purchase date — all three are mandatory.
          * For FD: principal (?), interest rate (%), tenure (months), bank name, and booking date — all five are mandatory.
          * NEVER guess, assume, or use default values for any of these fields. If the user hasn't provided a value, ASK for it.
          * Example: If user says "add 1 gram of gold", respond with "Sure! I need a couple more details: 1) Purchase rate per gram (?)? 2) Purchase date?"
        """;

    public ChatService(
        IAssetService assetService,
        IValuationService valuationService,
        IPriceProvider priceProvider,
        IConfiguration configuration)
    {
        var environment = configuration["SemanticKernel:Environment"] ?? "Development";
        var modelId = configuration["SemanticKernel:ModelId"] ?? "llama3.1";
        var apiKey = configuration["SemanticKernel:ApiKey"] ?? "ollama";
        var endpoint = configuration["SemanticKernel:Endpoint"] ?? "http://localhost:11434";
        var provider = configuration["SemanticKernel:Provider"] ?? "Ollama";

        var builder = Kernel.CreateBuilder();
        var fallbackOrder = new List<string>();

        // ??? Environment shortcut: Development/Local always forces local Ollama ???
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase) ||
            environment.Equals("Local", StringComparison.OrdinalIgnoreCase) || environment.Equals("Dev", StringComparison.OrdinalIgnoreCase))
        {
#pragma warning disable SKEXP0070
            builder.AddOllamaChatCompletion(
                modelId: modelId,
                endpoint: new Uri(endpoint));
#pragma warning restore SKEXP0070
        }
        else
        {
            // ??? Production / other environments honor Provider ???
            switch (provider.ToLowerInvariant())
            {
                case "ollama":
#pragma warning disable SKEXP0070
                    builder.AddOllamaChatCompletion(
                        modelId: modelId,
                        endpoint: new Uri(endpoint));
#pragma warning restore SKEXP0070
                    break;

                case "openai":
                    builder.AddOpenAIChatCompletion(modelId, apiKey);
                    break;

                case "openaicompat":
#pragma warning disable SKEXP0010
                    builder.AddOpenAIChatCompletion(
                        modelId: modelId,
                        apiKey: apiKey,
                        endpoint: new Uri(endpoint));
#pragma warning restore SKEXP0010
                    break;

                case "multi":
                {
                    // ??? Read OpenRouter config ???
                    var orApiKey = configuration["SemanticKernel:OpenRouter:ApiKey"]
                        ?? throw new InvalidOperationException(
                            "SemanticKernel:OpenRouter:ApiKey is required when Provider is Multi.");

                    var orEndpoint = configuration["SemanticKernel:OpenRouter:Endpoint"]
                        ?? "https://openrouter.ai/api/v1";

                    // Read the Models array from config (JSON array, not CSV)
                    var models = configuration.GetSection("SemanticKernel:OpenRouter:Models")
                        .Get<string[]>() ?? [];

                    if (models.Length == 0)
                        throw new InvalidOperationException(
                            "SemanticKernel:OpenRouter:Models must contain at least one model.");

                    // Register each OpenRouter model with an auto-generated serviceId
#pragma warning disable SKEXP0010
                    foreach (var model in models)
                    {
                        // e.g. "google/gemini-2.0-flash-exp" ? "or-google-gemini-2-0-flash-exp"
                        var serviceId = "or-" + model.Replace('/', '-').Replace('.', '-');

                        builder.AddOpenAIChatCompletion(
                            modelId: model,
                            apiKey: orApiKey,
                            endpoint: new Uri(orEndpoint),
                            serviceId: serviceId);

                        fallbackOrder.Add(serviceId);
                    }
#pragma warning restore SKEXP0010

                    // Optionally register local Ollama as final fallback
                    var localFallback = configuration.GetValue<bool>("SemanticKernel:LocalFallback", true);
                    if (localFallback)
                    {
#pragma warning disable SKEXP0070
                        const string localServiceId = "ollama-local";
                        builder.AddOllamaChatCompletion(
                            modelId: modelId,
                            endpoint: new Uri(endpoint),
                            serviceId: localServiceId);
                        fallbackOrder.Add(localServiceId);
#pragma warning restore SKEXP0070
                    }

                    break;
                }

                default:
                    throw new InvalidOperationException(
                        $"Unknown SemanticKernel:Provider '{provider}'. Use Ollama, OpenAI, OpenAICompat, or Multi.");
            }
        }

        _serviceFallbackOrder = fallbackOrder;
        _kernel = builder.Build();

        // Register plugins
        _assetsPlugin = new AssetsPlugin(assetService);
        _kernel.Plugins.AddFromObject(_assetsPlugin, "Assets");
        _kernel.Plugins.AddFromObject(new ValuationPlugin(valuationService), "Valuation");
        _kernel.Plugins.AddFromObject(new PricesPlugin(priceProvider), "Prices");
        _kernel.Plugins.AddFromObject(new HelpPlugin(), "Help");

        _chatHistory = new ChatHistory(SystemPrompt);
    }

    public async Task<ChatResult> ChatAsync(string userMessage)
    {
        _chatHistory.AddUserMessage(userMessage);
        _assetsPlugin.DataChanged = false;

        try
        {
            if (_serviceFallbackOrder.Count == 0)
            {
                // ??? Single provider path ???
                return await ExecuteChatAsync();
            }

            // ??? Multi-provider with fallback ???
            foreach (var serviceId in _serviceFallbackOrder)
            {
                var result = await TryChatWithServiceAsync(serviceId);
                if (result is not null)
                    return result;
            }

            // All services failed
            _chatHistory.RemoveAt(_chatHistory.Count - 1);
            return new ChatResult
            {
                Reply = "All configured AI models are currently unavailable (rate limits or connection issues). Please try again in a moment.",
                DataChanged = _assetsPlugin.DataChanged
            };
        }
        catch (Exception ex)
        {
            _chatHistory.RemoveAt(_chatHistory.Count - 1);
            return new ChatResult
            {
                Reply = $"[Error] {ex.GetType().Name}: {ex.Message}",
                DataChanged = _assetsPlugin.DataChanged
            };
        }
    }

    /// <summary>Single-provider chat (no serviceId routing).</summary>
    private async Task<ChatResult> ExecuteChatAsync()
    {
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
        var settings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var response = await chatCompletion.GetChatMessageContentAsync(
            _chatHistory, settings, _kernel);

        var content = response.Content ?? string.Empty;
        _chatHistory.AddAssistantMessage(content);

        return new ChatResult
        {
            Reply = string.IsNullOrWhiteSpace(content)
                ? "I processed your request but have nothing to add."
                : content,
            DataChanged = _assetsPlugin.DataChanged
        };
    }

    /// <summary>Try a specific service by ID. Returns null on any transient/HTTP failure (caller tries next).</summary>
    private async Task<ChatResult?> TryChatWithServiceAsync(string serviceId)
    {
        try
        {
            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>(serviceId);
            var settings = new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var response = await chatCompletion.GetChatMessageContentAsync(
                _chatHistory, settings, _kernel);

            var content = response.Content ?? string.Empty;

            // Guard: if model returned empty/null, treat as failure and try next
            if (string.IsNullOrWhiteSpace(content))
                return null;

            _chatHistory.AddAssistantMessage(content);

            return new ChatResult
            {
                Reply = content,
                DataChanged = _assetsPlugin.DataChanged
            };
        }
        catch (HttpOperationException)
        {
            // Semantic Kernel wraps HTTP errors (404, 429, 503, etc.) in this type
            return null;
        }
        catch (HttpRequestException)
        {
            // Raw network / DNS / connection failures
            return null;
        }
        catch (TaskCanceledException)
        {
            // Timeout
            return null;
        }
    }

    public void ResetChat()
    {
        _chatHistory.Clear();
        _chatHistory.AddSystemMessage(SystemPrompt);
    }
}
