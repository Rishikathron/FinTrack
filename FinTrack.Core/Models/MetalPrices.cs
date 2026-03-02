namespace FinTrack.Models;

/// <summary>
/// Holds current metal prices per gram in INR with daily change.
/// </summary>
public class MetalPrices
{
    public decimal GoldPricePerGram { get; set; }
    public decimal SilverPricePerGram { get; set; }

    /// <summary>Gold daily change in % (e.g., +1.23 or -0.45).</summary>
    public decimal GoldDailyChangePercent { get; set; }

    /// <summary>Silver daily change in % (e.g., +1.23 or -0.45).</summary>
    public decimal SilverDailyChangePercent { get; set; }

    public DateTime FetchedAt { get; set; }
}
