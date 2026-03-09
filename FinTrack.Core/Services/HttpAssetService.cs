using System.Net.Http.Json;
using FinTrack.Interfaces;
using FinTrack.Models;

namespace FinTrack.Core.Services;

/// <summary>
/// HTTP-backed IAssetService that calls the FinTrack API.
/// Used by Telegram bot in production to share data with the API service.
/// </summary>
public class HttpAssetService(HttpClient httpClient) : IAssetService
{
    public async Task<List<Asset>> GetAssetsAsync(string userId)
    {
        var result = await httpClient.GetFromJsonAsync<List<Asset>>("api/Assets/list");
        return result ?? [];
    }

    public async Task<Asset?> GetAssetByIdAsync(string userId, Guid assetId)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<Asset>($"api/Assets/detail/{assetId}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Asset> AddAssetAsync(string userId, AddAssetRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("api/Assets/add", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Asset>())!;
    }

    public async Task<Asset?> UpdateAssetAsync(string userId, Guid assetId, UpdateAssetRequest request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/Assets/edit/{assetId}", request);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Asset>();
    }

    public async Task<bool> DeleteAssetAsync(string userId, Guid assetId)
    {
        var response = await httpClient.DeleteAsync($"api/Assets/remove/{assetId}");
        return response.IsSuccessStatusCode;
    }
}
