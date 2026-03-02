using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FinTrack.Models;

/// <summary>
/// Request body for adding a new asset.
/// </summary>
public class AddAssetRequest
{
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AssetType Type { get; set; }

    /// <summary>Grams of gold/silver. Required when Type is Gold or Silver.</summary>
    public decimal Quantity { get; set; }

    /// <summary>INR amount (principal for FD). Required when Type is FD.</summary>
    public decimal Amount { get; set; }

    /// <summary>Date of purchase/booking. Defaults to today if not provided.</summary>
    public DateTime? PurchaseDate { get; set; }

    /// <summary>Purchase rate per gram in INR (for Gold/Silver).</summary>
    public decimal PurchaseRatePerGram { get; set; }

    // ??? FD-specific fields ???

    /// <summary>Annual interest rate in % (e.g., 7.5). FD only.</summary>
    public decimal InterestRate { get; set; }

    /// <summary>Tenure in months (e.g., 6, 12, 18). FD only.</summary>
    public int TenureMonths { get; set; }

    /// <summary>Bank name. FD only.</summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>Goal/purpose. FD only.</summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>Free-text notes. FD only.</summary>
    public string Notes { get; set; } = string.Empty;
}
