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

    /// <summary>Amount in INR (used for FD principal). Ignored for Gold/Silver.</summary>
    public decimal Amount { get; set; }

    /// <summary>Unit of measurement (e.g., "grams" for metals, "INR" for FD).</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Date when the asset was purchased/booked.</summary>
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    /// <summary>Purchase rate per gram in INR (used for Gold/Silver). Ignored for FD.</summary>
    public decimal PurchaseRatePerGram { get; set; }

    // ??? FD-specific fields ???

    /// <summary>Annual interest rate in % (e.g., 7.5). FD only.</summary>
    public decimal InterestRate { get; set; }

    /// <summary>Tenure in months (e.g., 6, 12, 18). FD only.</summary>
    public int TenureMonths { get; set; }

    /// <summary>Bank name (e.g., "HDFC", "SBI", "Union Bank"). FD only.</summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>Goal/purpose (e.g., "Emergency Fund", "Car Down Payment"). FD only.</summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>Free-text notes (e.g., "Can break anytime", "Auto-renewal"). FD only.</summary>
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
