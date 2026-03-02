namespace FinTrack.Models;

/// <summary>
/// Net worth summary with per-category totals.
/// </summary>
public class NetWorthSummary
{
    public decimal GoldValue { get; set; }
    public decimal SilverValue { get; set; }
    public decimal FDValue { get; set; }
    public decimal TotalNetWorth { get; set; }
}
