using FinTrack.Models;

namespace FinTrack.Interfaces;

/// <summary>
/// Fetches current metal prices. Designed to be reusable by Semantic Kernel plugins.
/// </summary>
public interface IPriceProvider
{
    Task<MetalPrices> GetCurrentPricesAsync();
}
