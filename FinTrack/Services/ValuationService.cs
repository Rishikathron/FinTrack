using FinTrack.Interfaces;
using FinTrack.Models;

namespace FinTrack.Services;

/// <summary>
/// Calculates asset valuations using live metal prices.
/// Uses strategy-style approach: each asset type has its own valuation logic.
/// </summary>
public class ValuationService : IValuationService
{
    private readonly IAssetService _assetService;
    private readonly IPriceProvider _priceProvider;

    public ValuationService(IAssetService assetService, IPriceProvider priceProvider)
    {
        _assetService = assetService;
        _priceProvider = priceProvider;
    }

    public async Task<NetWorthSummary> GetNetWorthAsync(string userId)
    {
        var breakdown = await GetBreakdownAsync(userId);

        return new NetWorthSummary
        {
            GoldValue = breakdown.Where(a => a.Type == AssetType.Gold).Sum(a => a.TotalValue),
            SilverValue = breakdown.Where(a => a.Type == AssetType.Silver).Sum(a => a.TotalValue),
            FDValue = breakdown.Where(a => a.Type == AssetType.FD).Sum(a => a.TotalValue),
            TotalNetWorth = breakdown.Sum(a => a.TotalValue)
        };
    }

    public async Task<List<AssetValuation>> GetBreakdownAsync(string userId)
    {
        var assets = await _assetService.GetAssetsAsync(userId);
        var prices = await _priceProvider.GetCurrentPricesAsync();

        var valuations = new List<AssetValuation>();

        foreach (var asset in assets)
        {
            var valuation = CalculateValuation(asset, prices);
            valuations.Add(valuation);
        }

        return valuations;
    }

    /// <summary>
    /// Strategy-style valuation per asset type.
    /// Add new cases here when adding Crypto, Stocks, etc.
    /// </summary>
    private static AssetValuation CalculateValuation(Asset asset, MetalPrices prices)
    {
        return asset.Type switch
        {
            AssetType.Gold => new AssetValuation
            {
                AssetId = asset.Id,
                Type = asset.Type,
                Quantity = asset.Quantity,
                PricePerUnit = prices.GoldPricePerGram,
                TotalValue = Math.Round(asset.Quantity * prices.GoldPricePerGram, 2)
            },
            AssetType.Silver => new AssetValuation
            {
                AssetId = asset.Id,
                Type = asset.Type,
                Quantity = asset.Quantity,
                PricePerUnit = prices.SilverPricePerGram,
                TotalValue = Math.Round(asset.Quantity * prices.SilverPricePerGram, 2)
            },
            AssetType.FD => new AssetValuation
            {
                AssetId = asset.Id,
                Type = asset.Type,
                Quantity = 1,
                PricePerUnit = asset.Amount,
                TotalValue = asset.Amount
            },
            _ => throw new NotSupportedException($"Asset type '{asset.Type}' is not supported yet.")
        };
    }
}
