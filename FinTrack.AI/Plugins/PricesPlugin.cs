using System.ComponentModel;
using System.Text.Json;
using FinTrack.Interfaces;
using Microsoft.SemanticKernel;

namespace FinTrack.AI.Plugins;

/// <summary>
/// Semantic Kernel plugin for live metal prices.
/// Wraps IPriceProvider so the AI agent can report current gold/silver rates.
/// </summary>
public class PricesPlugin
{
    private readonly IPriceProvider _priceProvider;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PricesPlugin(IPriceProvider priceProvider)
    {
        _priceProvider = priceProvider;
    }

    [KernelFunction("get_current_prices")]
    [Description("Get live gold (22K) and silver prices per gram in INR, including daily percentage change. Returns JSON with goldPricePerGram, silverPricePerGram, goldDailyChangePercent, silverDailyChangePercent.")]
    public async Task<string> GetCurrentPricesAsync()
    {
        var prices = await _priceProvider.GetCurrentPricesAsync();
        return JsonSerializer.Serialize(prices, _jsonOptions);
    }
}
