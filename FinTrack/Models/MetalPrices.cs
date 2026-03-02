namespace FinTrack.Models;

/// <summary>
/// Holds current metal prices per gram in INR.
/// </summary>
public class MetalPrices
{
    public decimal GoldPricePerGram { get; set; }
    public decimal SilverPricePerGram { get; set; }
    public DateTime FetchedAt { get; set; }
}
