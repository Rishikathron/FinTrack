using System.Net.Http.Json;
using FinTrack.Interfaces;
using FinTrack.Models;

namespace FinTrack.Core.Services;

/// <summary>
/// HTTP-backed IValuationService that calls the FinTrack API.
/// Used by Telegram bot in production to share data with the API service.
/// </summary>
public class HttpValuationService(HttpClient httpClient) : IValuationService
{
    public async Task<NetWorthSummary> GetNetWorthAsync(string userId)
    {
        return (await httpClient.GetFromJsonAsync<NetWorthSummary>("api/Valuation/networth"))!;
    }

    public async Task<List<AssetValuation>> GetBreakdownAsync(string userId)
    {
        return (await httpClient.GetFromJsonAsync<List<AssetValuation>>("api/Valuation/breakdown")) ?? [];
    }
}
