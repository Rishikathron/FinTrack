using System.Net.Http.Json;
using FinTrack.Interfaces;
using FinTrack.Models;

namespace FinTrack.Core.Services;

/// <summary>
/// HTTP-backed IPriceProvider that calls the FinTrack API.
/// Used by Telegram bot in production to share data with the API service.
/// </summary>
public class HttpPriceProvider(HttpClient httpClient) : IPriceProvider
{
    public async Task<MetalPrices> GetCurrentPricesAsync()
    {
        return (await httpClient.GetFromJsonAsync<MetalPrices>("api/Prices/current"))!;
    }
}
