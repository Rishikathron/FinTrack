using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FinTrack.Models;

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

    /// <summary>INR amount. Required when Type is FD.</summary>
    public decimal Amount { get; set; }
}
