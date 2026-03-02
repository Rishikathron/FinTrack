namespace FinTrack.Models;

/// <summary>
/// Breakdown of each asset's current value.
/// </summary>
public class AssetValuation
{
    public Guid AssetId { get; set; }
    public AssetType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal TotalValue { get; set; }
}
