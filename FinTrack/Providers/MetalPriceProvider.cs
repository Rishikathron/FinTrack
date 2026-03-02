using FinTrack.Interfaces;
using FinTrack.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace FinTrack.Providers;

/// <summary>
/// Fetches gold and silver prices from MetalPriceAPI.
/// Caches results for 10 minutes to avoid excessive API calls.
/// 
/// Sign up at https://metalpriceapi.com/ for a free API key.
/// Set the key in appsettings.json under "MetalPriceApi:ApiKey".
/// 
/// If the API is unavailable, falls back to hardcoded prices.
/// </summary>
public class MetalPriceProvider : IPriceProvider
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<MetalPriceProvider> _logger;

    private const string CacheKey = "MetalPrices";
    private const decimal OunceToGrams = 31.1035m;

    // Fallback prices (approximate) if API is unavailable
    private const decimal FallbackGoldPerGram = 7500m;
    private const decimal FallbackSilverPerGram = 90m;

    public MetalPriceProvider(HttpClient httpClient, IMemoryCache cache,
        IConfiguration config, ILogger<MetalPriceProvider> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    public async Task<MetalPrices> GetCurrentPricesAsync()
    {
        // Return cached prices if available
        if (_cache.TryGetValue(CacheKey, out MetalPrices? cached) && cached is not null)
            return cached;

        var prices = await FetchFromApiAsync();

        // Cache for 10 minutes
        _cache.Set(CacheKey, prices, TimeSpan.FromMinutes(10));
        return prices;
    }

    private async Task<MetalPrices> FetchFromApiAsync()
    {
        var apiKey = _config["MetalPriceApi:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("MetalPriceApi:ApiKey not configured. Using fallback prices.");
            return GetFallbackPrices();
        }

        try
        {
            // MetalPriceAPI returns prices in USD per ounce. We convert to INR per gram.
            var url = $"https://api.metalpriceapi.com/v1/latest?api_key={apiKey}&base=INR&currencies=XAU,XAG";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var rates = doc.RootElement.GetProperty("rates");

            // API returns: 1 INR = X ounces of gold/silver
            // So price per ounce in INR = 1 / X
            // Price per gram = price per ounce / 31.1035
            var goldRate = rates.GetProperty("INRXAU").GetDecimal();
            var silverRate = rates.GetProperty("INRXAG").GetDecimal();

            var goldPerOunce = 1m / goldRate;
            var silverPerOunce = 1m / silverRate;

            var prices = new MetalPrices
            {
                GoldPricePerGram = Math.Round(goldPerOunce / OunceToGrams, 2),
                SilverPricePerGram = Math.Round(silverPerOunce / OunceToGrams, 2),
                FetchedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Fetched metal prices: Gold={Gold}/g, Silver={Silver}/g",
                prices.GoldPricePerGram, prices.SilverPricePerGram);

            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch metal prices from API. Using fallback.");
            return GetFallbackPrices();
        }
    }

    private static MetalPrices GetFallbackPrices() => new()
    {
        GoldPricePerGram = FallbackGoldPerGram,
        SilverPricePerGram = FallbackSilverPerGram,
        FetchedAt = DateTime.UtcNow
    };
}
