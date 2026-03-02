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

    public Task<Asset> AddAssetAsync(string userId, AddAssetRequest request)
    {
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            Quantity = request.Type is AssetType.Gold or AssetType.Silver ? request.Quantity : 0,
            Amount = request.Type == AssetType.FD ? request.Amount : 0,
            Unit = request.Type == AssetType.FD ? "INR" : "grams",
            CreatedAt = DateTime.UtcNow
        };

        var assets = _repository.Load(userId);
        assets.Add(asset);
        _repository.Save(userId, assets);

        return Task.FromResult(asset);
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
