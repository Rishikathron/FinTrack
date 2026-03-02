namespace FinTrack.Models;

/// <summary>
/// Breakdown of each asset's current value with profit/loss calculation.
/// </summary>
public class AssetValuation
{
    public Guid AssetId { get; set; }
    public AssetType Type { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>Current market rate per unit (per gram for metals, total for FD).</summary>
    public decimal PricePerUnit { get; set; }

    /// <summary>Current total market value.</summary>
    public decimal TotalValue { get; set; }

    /// <summary>Date of purchase.</summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>Rate per gram at time of purchase (for Gold/Silver).</summary>
    public decimal PurchaseRatePerGram { get; set; }

    /// <summary>Total cost at time of purchase.</summary>
    public decimal PurchaseValue { get; set; }

    /// <summary>Profit or loss in INR.</summary>
    public decimal ProfitLoss { get; set; }

    /// <summary>Profit or loss as percentage.</summary>
    public decimal ProfitLossPercent { get; set; }

    // ??? FD-specific valuation fields ???

    /// <summary>Annual interest rate (for FD).</summary>
    public decimal InterestRate { get; set; }

    /// <summary>Accrued interest so far (for FD).</summary>
    public decimal AccruedInterest { get; set; }

    /// <summary>Tenure in months (for FD).</summary>
    public int TenureMonths { get; set; }

    /// <summary>Maturity date computed from PurchaseDate + TenureMonths.</summary>
    public DateTime? MaturityDate { get; set; }

    /// <summary>Bank name (for FD).</summary>
    public string BankName { get; set; } = string.Empty;

    /// <summary>Goal/purpose (for FD).</summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>Notes (for FD).</summary>
    public string Notes { get; set; } = string.Empty;
}
