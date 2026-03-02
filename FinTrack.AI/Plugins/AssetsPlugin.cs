using System.ComponentModel;
using System.Text.Json;
using FinTrack.Interfaces;
using FinTrack.Models;
using Microsoft.SemanticKernel;

namespace FinTrack.AI.Plugins;

/// <summary>
/// Semantic Kernel plugin for asset CRUD operations.
/// Wraps IAssetService so the AI agent can manage gold, silver, and FD assets.
/// </summary>
public class AssetsPlugin
{
    private readonly IAssetService _assetService;
    private const string UserId = "default-user";

    /// <summary>
    /// Set to true when any mutation (add/update/delete) occurs during a chat turn.
    /// ChatService reads and resets this after each turn.
    /// </summary>
    public bool DataChanged { get; set; }

    // JSON options for returning readable data to the LLM
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AssetsPlugin(IAssetService assetService)
    {
        _assetService = assetService;
    }

    [KernelFunction("get_all_assets")]
    [Description("Get all assets (gold, silver, fixed deposits) for the current user. Returns JSON array with each asset's id, type, quantity, amount, purchaseDate, purchaseRatePerGram, interestRate, tenureMonths, bankName.")]
    public async Task<string> GetAllAssetsAsync()
    {
        var assets = await _assetService.GetAssetsAsync(UserId);
        return JsonSerializer.Serialize(assets, _jsonOptions);
    }

    [KernelFunction("get_asset_by_id")]
    [Description("Get a single asset by its unique ID")]
    public async Task<string> GetAssetByIdAsync(
        [Description("The asset GUID")] string assetId)
    {
        var asset = await _assetService.GetAssetByIdAsync(UserId, Guid.Parse(assetId));
        return asset is not null
            ? JsonSerializer.Serialize(asset, _jsonOptions)
            : "Asset not found.";
    }

    [KernelFunction("add_gold")]
    [Description("Add a gold asset with weight in grams and purchase rate per gram in INR")]
    public async Task<string> AddGoldAsync(
        [Description("Weight in grams (e.g., 8, 10.5)")] decimal quantity,
        [Description("Purchase rate in INR per gram (e.g., 7200)")] decimal purchaseRatePerGram,
        [Description("Purchase date in YYYY-MM-DD format. Defaults to today if not provided.")] string? purchaseDate = null)
    {
        var asset = await _assetService.AddAssetAsync(UserId, new AddAssetRequest
        {
            Type = AssetType.Gold,
            Quantity = quantity,
            PurchaseRatePerGram = purchaseRatePerGram,
            PurchaseDate = purchaseDate != null ? DateTime.Parse(purchaseDate) : null
        });
        DataChanged = true;
        return $"Gold asset added: {asset.Quantity}g at ?{asset.PurchaseRatePerGram}/g (ID: {asset.Id})";
    }

    [KernelFunction("add_silver")]
    [Description("Add a silver asset with weight in grams and purchase rate per gram in INR")]
    public async Task<string> AddSilverAsync(
        [Description("Weight in grams (e.g., 500, 1000)")] decimal quantity,
        [Description("Purchase rate in INR per gram (e.g., 95)")] decimal purchaseRatePerGram,
        [Description("Purchase date in YYYY-MM-DD format. Defaults to today if not provided.")] string? purchaseDate = null)
    {
        var asset = await _assetService.AddAssetAsync(UserId, new AddAssetRequest
        {
            Type = AssetType.Silver,
            Quantity = quantity,
            PurchaseRatePerGram = purchaseRatePerGram,
            PurchaseDate = purchaseDate != null ? DateTime.Parse(purchaseDate) : null
        });
        DataChanged = true;
        return $"Silver asset added: {asset.Quantity}g at ?{asset.PurchaseRatePerGram}/g (ID: {asset.Id})";
    }

    [KernelFunction("add_fd")]
    [Description("Create a fixed deposit with principal amount, interest rate, tenure, and bank name")]
    public async Task<string> AddFdAsync(
        [Description("Principal amount in INR (e.g., 200000)")] decimal amount,
        [Description("Annual interest rate in percent (e.g., 7.5)")] decimal interestRate,
        [Description("Tenure in months (e.g., 12, 18, 24)")] int tenureMonths,
        [Description("Bank name (e.g., HDFC, SBI, Union Bank)")] string bankName,
        [Description("Goal or purpose (e.g., Emergency Fund). Optional.")] string? goal = null,
        [Description("Additional notes. Optional.")] string? notes = null,
        [Description("Booking date in YYYY-MM-DD format. Defaults to today if not provided.")] string? purchaseDate = null)
    {
        var asset = await _assetService.AddAssetAsync(UserId, new AddAssetRequest
        {
            Type = AssetType.FD,
            Amount = amount,
            InterestRate = interestRate,
            TenureMonths = tenureMonths,
            BankName = bankName,
            Goal = goal ?? string.Empty,
            Notes = notes ?? string.Empty,
            PurchaseDate = purchaseDate != null ? DateTime.Parse(purchaseDate) : null
        });
        DataChanged = true;
        return $"FD added: ?{asset.Amount} at {asset.InterestRate}% for {asset.TenureMonths} months in {asset.BankName} (ID: {asset.Id})";
    }

    [KernelFunction("delete_asset")]
    [Description("Delete a single asset by its unique GUID")]
    public async Task<string> DeleteAssetAsync(
        [Description("The asset GUID to delete")] string assetId)
    {
        var deleted = await _assetService.DeleteAssetAsync(UserId, Guid.Parse(assetId));
        if (deleted) DataChanged = true;
        return deleted ? "Asset deleted successfully." : "Asset not found.";
    }

    [KernelFunction("delete_assets_by_type")]
    [Description("Delete ALL assets of a specific type. Use this when user says 'remove all gold', 'delete entire silver', 'remove all FDs', etc.")]
    public async Task<string> DeleteAssetsByTypeAsync(
        [Description("Asset type to delete: Gold, Silver, or FD")] string assetType)
    {
        if (!Enum.TryParse<AssetType>(assetType, ignoreCase: true, out var type))
            return $"Invalid asset type '{assetType}'. Use Gold, Silver, or FD.";

        var assets = await _assetService.GetAssetsAsync(UserId);
        var toDelete = assets.Where(a => a.Type == type).ToList();

        if (toDelete.Count == 0)
            return $"No {type} assets found to delete.";

        var count = 0;
        foreach (var asset in toDelete)
        {
            if (await _assetService.DeleteAssetAsync(UserId, asset.Id))
                count++;
        }

        if (count > 0) DataChanged = true;
        return $"Deleted {count} {type} asset(s).";
    }

    [KernelFunction("update_asset")]
    [Description("Update an existing asset by ID. For gold/silver: update quantity, rate, date. For FD: update amount, rate, tenure, bank, goal, notes.")]
    public async Task<string> UpdateAssetAsync(
        [Description("The asset GUID to update")] string assetId,
        [Description("Updated quantity in grams (for gold/silver) or 0")] decimal quantity = 0,
        [Description("Updated amount in INR (for FD) or 0")] decimal amount = 0,
        [Description("Updated purchase rate per gram (for gold/silver) or 0")] decimal purchaseRatePerGram = 0,
        [Description("Updated purchase date in YYYY-MM-DD format. Optional.")] string? purchaseDate = null,
        [Description("Updated annual interest rate (for FD) or 0")] decimal interestRate = 0,
        [Description("Updated tenure in months (for FD) or 0")] int tenureMonths = 0,
        [Description("Updated bank name (for FD). Optional.")] string? bankName = null,
        [Description("Updated goal (for FD). Optional.")] string? goal = null,
        [Description("Updated notes (for FD). Optional.")] string? notes = null)
    {
        var updated = await _assetService.UpdateAssetAsync(UserId, Guid.Parse(assetId), new UpdateAssetRequest
        {
            Quantity = quantity,
            Amount = amount,
            PurchaseRatePerGram = purchaseRatePerGram,
            PurchaseDate = purchaseDate != null ? DateTime.Parse(purchaseDate) : null,
            InterestRate = interestRate,
            TenureMonths = tenureMonths,
            BankName = bankName ?? string.Empty,
            Goal = goal ?? string.Empty,
            Notes = notes ?? string.Empty
        });
        if (updated is not null) DataChanged = true;
        return updated is not null ? "Asset updated successfully." : "Asset not found.";
    }
}
