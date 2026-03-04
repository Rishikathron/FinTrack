using FinTrack.Interfaces;
using FinTrack.Models;
using Microsoft.SemanticKernel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    [Description("Add a gold purchase. IMPORTANT: Do NOT call this unless the user has explicitly stated ALL of: grams, purchase rate per gram, and purchase date. IMPORTANT: If ANY of these three values is missing from the user's message, you MUST ask the user for the missing values before calling this function. Never guess or assume values.")]
    public async Task<string> AddGoldAsync(
        [Description("Weight in grams as stated by the user (e.g., 8, 10.5).IMPORTANT: Must be provided by user.")] decimal quantity,
        [Description("Purchase rate in INR per gram as stated. IMPORTANT: must be provided by user — never assume or guess this value.")] decimal purchaseRatePerGram,
        [Description("Purchase date in YYYY-MM-DD format as stated by the user. Must be provided by user")] string purchaseDate)
    {
        var asset = await _assetService.AddAssetAsync(UserId, new AddAssetRequest
        {
            Type = AssetType.Gold,
            Quantity = quantity,
            PurchaseRatePerGram = purchaseRatePerGram,
            PurchaseDate = purchaseDate == null ? DateTime.Now : DateTime.Parse(purchaseDate)
        });
        DataChanged = true;
        return $"Gold asset added: {asset.Quantity}g at ?{asset.PurchaseRatePerGram}/g on {asset.PurchaseDate:yyyy-MM-dd} (ID: {asset.Id})";
    }

    [KernelFunction("add_silver")]
    [Description("Add a silver purchase. IMPORTANT: Do NOT call this unless the user has explicitly stated ALL of: grams, purchase rate per gram, and purchase date. If ANY of these three values is missing from the user's message, you MUST ask the user for the missing values before calling this function. Never guess or assume values.")]
    public async Task<string> AddSilverAsync(
        [Description("Weight in grams as stated by the user (e.g., 500, 1000). Must be provided by user.")] decimal quantity,
        [Description("Purchase rate in INR per gram as stated by the user (e.g., 95). Must be provided by user — never assume or guess this value.")] decimal purchaseRatePerGram,
        [Description("Purchase date in YYYY-MM-DD format as stated by the user. Must be provided by user — never default to today.")] string purchaseDate)
    {
        var asset = await _assetService.AddAssetAsync(UserId, new AddAssetRequest
        {
            Type = AssetType.Silver,
            Quantity = quantity,
            PurchaseRatePerGram = purchaseRatePerGram,
            PurchaseDate = purchaseDate == null ? DateTime.Now : DateTime.Parse(purchaseDate)
        });
        DataChanged = true;
        return $"Silver asset added: {asset.Quantity}g at ?{asset.PurchaseRatePerGram}/g on {asset.PurchaseDate:yyyy-MM-dd} (ID: {asset.Id})";
    }

    [KernelFunction("add_fd")]
    [Description("Create a fixed deposit. IMPORTANT: Do NOT call this unless the user has explicitly stated ALL of: principal amount, interest rate, tenure, and bank name. If ANY of these values is missing from the user's message, you MUST ask the user for the missing values before calling this function. Never guess or assume values.")]
    public async Task<string> AddFdAsync(
        [Description("Principal amount in INR as stated by user (e.g., 200000). Must be provided by user.")] decimal amount,
        [Description("Annual interest rate in percent as stated by user (e.g., 7.5). Must be provided by user.")] decimal interestRate,
        [Description("Tenure in months as stated by user (e.g., 12, 18, 24). Must be provided by user.")] int tenureMonths,
        [Description("Bank name as stated by user (e.g., HDFC, SBI). Must be provided by user.")] string bankName,
        [Description("Goal or purpose if stated by user. Optional — only include if user mentioned it.")] string? goal = null,
        [Description("Additional notes if stated by user. Optional — only include if user mentioned it.")] string? notes = null,
        [Description("Booking date in YYYY-MM-DD format as stated by user. Must be provided by user — never default to today.")] string purchaseDate = "")
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
            PurchaseDate = !string.IsNullOrWhiteSpace(purchaseDate) ? DateTime.Parse(purchaseDate) : null
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
