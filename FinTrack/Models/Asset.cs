using System.Text.Json.Serialization;

namespace FinTrack.Models;

/// <summary>
/// Represents a single asset entry (gold, silver, or fixed deposit).
/// </summary>
public class Asset
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AssetType Type { get; set; }

    /// <summary>Quantity in grams (used for Gold/Silver). Ignored for FD.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Amount in INR (used for FD). Ignored for Gold/Silver.</summary>
    public decimal Amount { get; set; }

    /// <summary>Unit of measurement (e.g., "grams" for metals, "INR" for FD).</summary>
    public string Unit { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
