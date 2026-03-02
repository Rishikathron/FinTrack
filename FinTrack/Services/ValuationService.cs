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

        var goldAssets = breakdown.Where(a => a.Type == AssetType.Gold).ToList();
        var silverAssets = breakdown.Where(a => a.Type == AssetType.Silver).ToList();
        var fdAssets = breakdown.Where(a => a.Type == AssetType.FD).ToList();

        var goldValue = goldAssets.Sum(a => a.TotalValue);
        var goldInvested = goldAssets.Sum(a => a.PurchaseValue);
        var goldPL = goldValue - goldInvested;
        var goldTotalGrams = goldAssets.Sum(a => a.Quantity);

        var silverValue = silverAssets.Sum(a => a.TotalValue);
        var silverInvested = silverAssets.Sum(a => a.PurchaseValue);
        var silverPL = silverValue - silverInvested;
        var silverTotalGrams = silverAssets.Sum(a => a.Quantity);

        var fdValue = fdAssets.Sum(a => a.TotalValue);
        var fdPrincipal = fdAssets.Sum(a => a.PurchaseValue);
        var fdInterest = fdAssets.Sum(a => a.AccruedInterest);

        // Bank-wise breakdown for dashboard
        var bankBreakdown = fdAssets
            .GroupBy(a => string.IsNullOrWhiteSpace(a.BankName) ? "Unknown" : a.BankName)
            .Select(g => new FDBankSummary
            {
                BankName = g.Key,
                Count = g.Count(),
                Principal = Math.Round(g.Sum(a => a.PurchaseValue), 2),
                CurrentValue = Math.Round(g.Sum(a => a.TotalValue), 2),
                AccruedInterest = Math.Round(g.Sum(a => a.AccruedInterest), 2)
            })
            .OrderByDescending(b => b.CurrentValue)
            .ToList();

        var totalValue = goldValue + silverValue + fdValue;
        var totalInvested = goldInvested + silverInvested + fdPrincipal;
        var totalPL = totalValue - totalInvested;

        return new NetWorthSummary
        {
            GoldValue = goldValue,
            SilverValue = silverValue,
            FDValue = fdValue,
            TotalNetWorth = totalValue,

            GoldTotalGrams = Math.Round(goldTotalGrams, 2),
            GoldSovereigns = Math.Round(goldTotalGrams / 8m, 2),
            GoldInvested = Math.Round(goldInvested, 2),
            GoldProfitLoss = Math.Round(goldPL, 2),
            GoldProfitLossPercent = goldInvested > 0 ? Math.Round(goldPL / goldInvested * 100, 2) : 0,

            SilverTotalGrams = Math.Round(silverTotalGrams, 2),
            SilverInvested = Math.Round(silverInvested, 2),
            SilverProfitLoss = Math.Round(silverPL, 2),
            SilverProfitLossPercent = silverInvested > 0 ? Math.Round(silverPL / silverInvested * 100, 2) : 0,

            FDPrincipal = Math.Round(fdPrincipal, 2),
            FDAccruedInterest = Math.Round(fdInterest, 2),
            FDCount = fdAssets.Count,
            FDBankBreakdown = bankBreakdown,

            TotalInvested = Math.Round(totalInvested, 2),
            TotalProfitLoss = Math.Round(totalPL, 2),
            TotalProfitLossPercent = totalInvested > 0 ? Math.Round(totalPL / totalInvested * 100, 2) : 0
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
            AssetType.Gold => BuildMetalValuation(asset, prices.GoldPricePerGram),
            AssetType.Silver => BuildMetalValuation(asset, prices.SilverPricePerGram),
            AssetType.FD => BuildFdValuation(asset),
            _ => throw new NotSupportedException($"Asset type '{asset.Type}' is not supported yet.")
        };
    }

    private static AssetValuation BuildMetalValuation(Asset asset, decimal currentPricePerGram)
    {
        var totalValue = Math.Round(asset.Quantity * currentPricePerGram, 2);
        var purchaseValue = Math.Round(asset.Quantity * asset.PurchaseRatePerGram, 2);
        var profitLoss = Math.Round(totalValue - purchaseValue, 2);
        var profitLossPercent = purchaseValue > 0
            ? Math.Round(profitLoss / purchaseValue * 100, 2)
            : 0;

        return new AssetValuation
        {
            AssetId = asset.Id,
            Type = asset.Type,
            Quantity = asset.Quantity,
            PricePerUnit = currentPricePerGram,
            TotalValue = totalValue,
            PurchaseDate = asset.PurchaseDate,
            PurchaseRatePerGram = asset.PurchaseRatePerGram,
            PurchaseValue = purchaseValue,
            ProfitLoss = profitLoss,
            ProfitLossPercent = profitLossPercent
        };
    }

    /// <summary>
    /// Compute FD current value using compound interest:
    /// CurrentValue = Principal × (1 + rate/100)^elapsedYears
    /// Capped at maturity (PurchaseDate + TenureMonths).
    /// </summary>
    private static AssetValuation BuildFdValuation(Asset asset)
    {
        var principal = asset.Amount;
        var rate = asset.InterestRate;
        var maturityDate = asset.TenureMonths > 0
            ? asset.PurchaseDate.AddMonths(asset.TenureMonths)
            : (DateTime?)null;

        // Elapsed years (capped at tenure if matured)
        var now = DateTime.UtcNow;
        var effectiveEnd = maturityDate.HasValue && now > maturityDate.Value
            ? maturityDate.Value
            : now;
        var elapsedYears = (decimal)(effectiveEnd - asset.PurchaseDate).TotalDays / 365.25m;
        if (elapsedYears < 0) elapsedYears = 0;

        // Compound interest: A = P × (1 + r/100)^t
        decimal currentValue;
        if (rate > 0 && elapsedYears > 0)
        {
            currentValue = principal * (decimal)Math.Pow((double)(1 + rate / 100m), (double)elapsedYears);
            currentValue = Math.Round(currentValue, 2);
        }
        else
        {
            currentValue = principal;
        }

        var accruedInterest = Math.Round(currentValue - principal, 2);

        return new AssetValuation
        {
            AssetId = asset.Id,
            Type = asset.Type,
            Quantity = 1,
            PricePerUnit = currentValue,
            TotalValue = currentValue,
            PurchaseDate = asset.PurchaseDate,
            PurchaseRatePerGram = 0,
            PurchaseValue = principal,
            ProfitLoss = accruedInterest,
            ProfitLossPercent = principal > 0 ? Math.Round(accruedInterest / principal * 100, 2) : 0,
            // FD-specific
            InterestRate = rate,
            AccruedInterest = accruedInterest,
            TenureMonths = asset.TenureMonths,
            MaturityDate = maturityDate,
            BankName = asset.BankName,
            Goal = asset.Goal,
            Notes = asset.Notes
        };
    }
}
