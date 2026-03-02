using FinTrack.Interfaces;
using FinTrack.Models;
using FinTrack.Storage;

namespace FinTrack.Services;

/// <summary>
/// Handles asset CRUD operations using JSON file storage.
/// </summary>
public class AssetService : IAssetService
{
    private readonly JsonFileRepository _repository;

    public AssetService(JsonFileRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Asset>> GetAssetsAsync(string userId)
    {
        var assets = _repository.Load(userId);
        return Task.FromResult(assets);
    }

    public Task<Asset?> GetAssetByIdAsync(string userId, Guid assetId)
    {
        var assets = _repository.Load(userId);
        var asset = assets.FirstOrDefault(a => a.Id == assetId);
        return Task.FromResult(asset);
    }

    public Task<Asset> AddAssetAsync(string userId, AddAssetRequest request)
    {
        var isFd = request.Type == AssetType.FD;
        var isMetal = request.Type is AssetType.Gold or AssetType.Silver;

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            Quantity = isMetal ? request.Quantity : 0,
            Amount = isFd ? request.Amount : 0,
            Unit = isFd ? "INR" : "grams",
            PurchaseDate = request.PurchaseDate ?? DateTime.UtcNow,
            PurchaseRatePerGram = isMetal ? request.PurchaseRatePerGram : 0,
            // FD-specific
            InterestRate = isFd ? request.InterestRate : 0,
            TenureMonths = isFd ? request.TenureMonths : 0,
            BankName = isFd ? request.BankName : string.Empty,
            Goal = isFd ? request.Goal : string.Empty,
            Notes = isFd ? request.Notes : string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        var assets = _repository.Load(userId);
        assets.Add(asset);
        _repository.Save(userId, assets);

        return Task.FromResult(asset);
    }

    public Task<Asset?> UpdateAssetAsync(string userId, Guid assetId, UpdateAssetRequest request)
    {
        var assets = _repository.Load(userId);
        var asset = assets.FirstOrDefault(a => a.Id == assetId);

        if (asset is null) return Task.FromResult<Asset?>(null);

        if (asset.Type is AssetType.Gold or AssetType.Silver)
        {
            asset.Quantity = request.Quantity;
            asset.PurchaseRatePerGram = request.PurchaseRatePerGram;
        }
        else if (asset.Type == AssetType.FD)
        {
            asset.Amount = request.Amount;
            asset.InterestRate = request.InterestRate;
            asset.TenureMonths = request.TenureMonths;
            asset.BankName = request.BankName;
            asset.Goal = request.Goal;
            asset.Notes = request.Notes;
        }

        if (request.PurchaseDate.HasValue)
        {
            asset.PurchaseDate = request.PurchaseDate.Value;
        }

        _repository.Save(userId, assets);
        return Task.FromResult<Asset?>(asset);
    }

    public Task<bool> DeleteAssetAsync(string userId, Guid assetId)
    {
        var assets = _repository.Load(userId);
        var removed = assets.RemoveAll(a => a.Id == assetId);

        if (removed == 0) return Task.FromResult(false);

        _repository.Save(userId, assets);
        return Task.FromResult(true);
    }
}
