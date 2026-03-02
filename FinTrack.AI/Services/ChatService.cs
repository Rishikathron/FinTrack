using FinTrack.AI.Plugins;
using FinTrack.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FinTrack.AI.Services;

/// <summary>
/// Result of a single chat turn, including the AI reply and whether asset data was mutated.
/// </summary>
public class ChatResult
{
    public string Reply { get; set; } = string.Empty;
    public bool DataChanged { get; set; }
}

/// <summary>
/// Orchestrates Semantic Kernel chat with FinTrack plugins.
/// Maintains per-session chat history and auto-invokes plugin functions.
/// 
/// Supports three modes via configuration:
///   1. OpenAI API: Provider=OpenAI, set ApiKey
///   2. Ollama (native): Provider=Ollama (default), set Endpoint to http://localhost:11434
///   3. OpenAI-compatible: Provider=OpenAICompat, set Endpoint and ApiKey
/// </summary>
public class ChatService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly ChatHistory _chatHistory;
    private readonly AssetsPlugin _assetsPlugin;

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
        - Always show amounts in INR (?) with Indian formatting
        - Gold is tracked in grams (22K purity, 8 grams = 1 sovereign)
        - Silver is tracked in grams
        - FDs use compound interest: A = P × (1 + r/100)^t
        - Be concise and helpful. Use bullet points for lists.
        - When the user asks about their portfolio, fetch live data — don't guess.
        - If a function call fails, explain what went wrong clearly.
        - IMPORTANT: You MUST use the provided tools/functions to perform actions. Never print JSON function calls as text.
        """;

    public ChatService(
        IAssetService assetService,
        IValuationService valuationService,
        IPriceProvider priceProvider,
        IConfiguration configuration)
    {
        var modelId = configuration["SemanticKernel:ModelId"] ?? "llama3.1";
        var apiKey = configuration["SemanticKernel:ApiKey"] ?? "ollama";
        var endpoint = configuration["SemanticKernel:Endpoint"] ?? "http://localhost:11434";
        var provider = configuration["SemanticKernel:Provider"] ?? "Ollama";

        var builder = Kernel.CreateBuilder();

        switch (provider.ToLowerInvariant())
        {
            case "ollama":
#pragma warning disable SKEXP0070 // Ollama connector is experimental
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

            default:
                throw new InvalidOperationException(
                    $"Unknown SemanticKernel:Provider '{provider}'. Use Ollama, OpenAI, or OpenAICompat.");
        }

        _kernel = builder.Build();

        // Register plugins — keep reference to AssetsPlugin to read DataChanged
        _assetsPlugin = new AssetsPlugin(assetService);
        _kernel.Plugins.AddFromObject(_assetsPlugin, "Assets");
        _kernel.Plugins.AddFromObject(new ValuationPlugin(valuationService), "Valuation");
        _kernel.Plugins.AddFromObject(new PricesPlugin(priceProvider), "Prices");

        _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        _chatHistory = new ChatHistory(SystemPrompt);
    }

    /// <summary>
    /// Send a user message and get AI response with automatic function calling.
    /// Returns both the reply and whether asset data was modified.
    /// </summary>
    public async Task<ChatResult> ChatAsync(string userMessage)
    {
        _chatHistory.AddUserMessage(userMessage);

        // Reset the flag before this turn
        _assetsPlugin.DataChanged = false;

        try
        {
            // Use base PromptExecutionSettings — works with ALL connectors (Ollama, OpenAI, etc.)
            var settings = new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var response = await _chatCompletion.GetChatMessageContentAsync(
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
        catch (HttpRequestException ex)
        {
            _chatHistory.RemoveAt(_chatHistory.Count - 1);
            return new ChatResult
            {
                Reply = $"[Connection error] Could not reach the AI model: {ex.Message}",
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

    /// <summary>
    /// Clear chat history and start fresh (keeps system prompt).
    /// </summary>
    public void ResetChat()
    {
        _chatHistory.Clear();
        _chatHistory.AddSystemMessage(SystemPrompt);
    }
}
