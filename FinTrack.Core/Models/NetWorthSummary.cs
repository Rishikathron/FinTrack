namespace FinTrack.Models;

/// <summary>
/// Net worth summary with per-category totals and profit/loss breakdown.
/// </summary>
public class NetWorthSummary
{
    // Current market values
    public decimal GoldValue { get; set; }
    public decimal SilverValue { get; set; }
    public decimal FDValue { get; set; }
    public decimal TotalNetWorth { get; set; }

    // Gold details
    public decimal GoldTotalGrams { get; set; }
    public decimal GoldSovereigns { get; set; }
    public decimal GoldInvested { get; set; }
    public decimal GoldProfitLoss { get; set; }
    public decimal GoldProfitLossPercent { get; set; }

    // Silver details
    public decimal SilverTotalGrams { get; set; }
    public decimal SilverInvested { get; set; }
    public decimal SilverProfitLoss { get; set; }
    public decimal SilverProfitLossPercent { get; set; }

    // FD details
    public decimal FDPrincipal { get; set; }
    public decimal FDAccruedInterest { get; set; }
    public int FDCount { get; set; }
    public List<FDBankSummary> FDBankBreakdown { get; set; } = new();

    // Overall totals
    public decimal TotalInvested { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public decimal TotalProfitLossPercent { get; set; }
}

/// <summary>
/// Per-bank FD summary for dashboard insight.
/// </summary>
public class FDBankSummary
{
    public string BankName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Principal { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal AccruedInterest { get; set; }
}
