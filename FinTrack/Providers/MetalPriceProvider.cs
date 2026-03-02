using FinTrack.Interfaces;
using FinTrack.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace FinTrack.Providers;

/// <summary>
/// Fetches gold and silver prices using Yahoo Finance (free, no API key required).
///
/// Sources:
///   GC=F  ? Gold Futures (USD per troy ounce)
///   SI=F  ? Silver Futures (USD per troy ounce)
///   INR=X ? USD/INR exchange rate
///
/// Conversion:
///   Price per gram (INR) = (USD per ounce × USD/INR) / 31.1035
///
/// An optional local premium percentage can be configured in appsettings.json
/// under "PriceSettings:LocalPremiumPercent" (default 0%).
///
/// Results are cached for 10 minutes. Falls back to hardcoded prices on failure.
/// </summary>
public class MetalPriceProvider : IPriceProvider
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<MetalPriceProvider> _logger;

    private const string CacheKey = "MetalPrices";
    private const decimal OunceToGrams = 31.1035m;

    // Gold purity conversion: COMEX GC=F is 24K (99.9% pure)
    // 22K = 22/24 of 24K price (91.67% purity), most common in India
    private const decimal Gold24KTo22KFactor = 22m / 24m;

    private const string YahooBaseUrl = "https://query1.finance.yahoo.com/v8/finance/chart";
    private const string GoldSymbol = "GC=F";
    private const string SilverSymbol = "SI=F";
    private const string UsdInrSymbol = "INR=X";

    // Fallback prices (approximate) if Yahoo Finance is unavailable
    private const decimal FallbackGoldPerGram = 7500m;
    private const decimal FallbackSilverPerGram = 90m;

    public MetalPriceProvider(HttpClient httpClient, IMemoryCache cache,
        IConfiguration config, ILogger<MetalPriceProvider> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _config = config;
        _logger = logger;

        // Yahoo Finance requires a User-Agent header
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }
    }

    public async Task<MetalPrices> GetCurrentPricesAsync()
    {
        // Return cached prices if available
        if (_cache.TryGetValue(CacheKey, out MetalPrices? cached) && cached is not null)
            return cached;

        var prices = await FetchFromYahooAsync();

        // Cache for 10 minutes
        _cache.Set(CacheKey, prices, TimeSpan.FromMinutes(10));
        return prices;
    }

    private async Task<MetalPrices> FetchFromYahooAsync()
    {
        try
        {
            // Fetch all three quotes in parallel
            var goldTask = FetchYahooQuoteAsync(GoldSymbol);
            var silverTask = FetchYahooQuoteAsync(SilverSymbol);
            var inrTask = FetchYahooQuoteAsync(UsdInrSymbol);

            await Task.WhenAll(goldTask, silverTask, inrTask);

            var goldQuote = await goldTask;
            var silverQuote = await silverTask;
            var inrQuote = await inrTask;

            var goldUsdPerOunce = goldQuote.Price;
            var silverUsdPerOunce = silverQuote.Price;
            var usdToInr = inrQuote.Price;

            _logger.LogInformation(
                "Yahoo Finance quotes — Gold: ${Gold}/oz, Silver: ${Silver}/oz, USD/INR: {Rate}",
                goldUsdPerOunce, silverUsdPerOunce, usdToInr);

            // Convert: (USD per ounce × USD/INR) / 31.1035 = INR per gram (24K)
            var gold24KInrPerGram = goldUsdPerOunce * usdToInr / OunceToGrams;
            var silverInrPerGram = silverUsdPerOunce * usdToInr / OunceToGrams;

            // Convert 24K ? 22K gold price (most common purity in India)
            var goldInrPerGram = gold24KInrPerGram * Gold24KTo22KFactor;

            // Apply optional local premium (default 0%)
            var premiumPercent = _config.GetValue<decimal>("PriceSettings:LocalPremiumPercent", 0m);
            if (premiumPercent > 0)
            {
                var multiplier = 1m + (premiumPercent / 100m);
                goldInrPerGram *= multiplier;
                silverInrPerGram *= multiplier;
            }

            // Compute daily % change from USD prices (previousClose ? current)
            var goldDailyChange = ComputeDailyChangePercent(goldQuote.Price, goldQuote.PreviousClose);
            var silverDailyChange = ComputeDailyChangePercent(silverQuote.Price, silverQuote.PreviousClose);

            var prices = new MetalPrices
            {
                GoldPricePerGram = Math.Round(goldInrPerGram, 2),
                SilverPricePerGram = Math.Round(silverInrPerGram, 2),
                GoldDailyChangePercent = goldDailyChange,
                SilverDailyChangePercent = silverDailyChange,
                FetchedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Metal prices (INR/g, premium {Premium}%): Gold(22K)={Gold} ({GoldChange}%), Silver={Silver} ({SilverChange}%)",
                premiumPercent, prices.GoldPricePerGram, goldDailyChange,
                prices.SilverPricePerGram, silverDailyChange);

            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch metal prices from Yahoo Finance. Using fallback.");
            return GetFallbackPrices();
        }
    }

    /// <summary>
    /// Computes daily % change: ((current - previousClose) / previousClose) × 100
    /// </summary>
    private static decimal ComputeDailyChangePercent(decimal current, decimal previousClose)
    {
        if (previousClose <= 0) return 0;
        return Math.Round((current - previousClose) / previousClose * 100, 2);
    }

    /// <summary>
    /// Fetches price + previousClose for a Yahoo Finance symbol.
    /// Response: { "chart": { "result": [{ "meta": { "regularMarketPrice": .., "previousClose": .. } }] } }
    /// </summary>
    private async Task<YahooQuote> FetchYahooQuoteAsync(string symbol)
    {
        var url = $"{YahooBaseUrl}/{symbol}?range=1d&interval=1d";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var meta = doc.RootElement
            .GetProperty("chart")
            .GetProperty("result")[0]
            .GetProperty("meta");

        var price = meta.GetProperty("regularMarketPrice").GetDecimal();

        // previousClose may not always be present; default to price (0% change)
        decimal previousClose = price;
        if (meta.TryGetProperty("chartPreviousClose", out var prevProp))
        {
            previousClose = prevProp.GetDecimal();
        }
        else if (meta.TryGetProperty("previousClose", out var prevProp2))
        {
            previousClose = prevProp2.GetDecimal();
        }

        return new YahooQuote(price, previousClose);
    }

    private static MetalPrices GetFallbackPrices() => new()
    {
        GoldPricePerGram = FallbackGoldPerGram,
        SilverPricePerGram = FallbackSilverPerGram,
        GoldDailyChangePercent = 0,
        SilverDailyChangePercent = 0,
        FetchedAt = DateTime.UtcNow
    };

    /// <summary>Simple record to hold price + previousClose from Yahoo.</summary>
    private sealed record YahooQuote(decimal Price, decimal PreviousClose);
}
