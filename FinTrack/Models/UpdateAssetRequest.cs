using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

/// <summary>
/// Request body for updating an existing asset.
/// </summary>
public class UpdateAssetRequest
{
    /// <summary>Updated grams of gold/silver. Used when asset Type is Gold or Silver.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Quantity must be non-negative.")]
    public decimal Quantity { get; set; }

    /// <summary>Updated INR amount. Used when asset Type is FD.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Amount must be non-negative.")]
    public decimal Amount { get; set; }

    /// <summary>Updated purchase date.</summary>
    public DateTime? PurchaseDate { get; set; }

    /// <summary>Updated purchase rate per gram in INR (for Gold/Silver).</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Purchase rate must be non-negative.")]
    public decimal PurchaseRatePerGram { get; set; }

    // ??? FD-specific fields ???

    /// <summary>Updated annual interest rate in %.</summary>
    [Range(0, 100, ErrorMessage = "Interest rate must be between 0 and 100.")]
    public decimal InterestRate { get; set; }

    /// <summary>Updated tenure in months.</summary>
    public int TenureMonths { get; set; }

    /// <summary>Updated bank name.</summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>Updated goal/purpose.</summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>Updated notes.</summary>
    public string Notes { get; set; } = string.Empty;
}
