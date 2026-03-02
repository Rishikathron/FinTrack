using FinTrack.Models;

namespace FinTrack.Interfaces;

/// <summary>
/// Manages asset CRUD operations. Designed to be reusable by Semantic Kernel plugins.
/// </summary>
public interface IAssetService
{
    Task<List<Asset>> GetAssetsAsync(string userId);
    Task<Asset> AddAssetAsync(string userId, AddAssetRequest request);
    Task<bool> DeleteAssetAsync(string userId, Guid assetId);
}
