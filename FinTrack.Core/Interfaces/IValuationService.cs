using FinTrack.Models;

namespace FinTrack.Interfaces;

/// <summary>
/// Calculates asset valuations and net worth. Designed to be reusable by Semantic Kernel plugins.
/// </summary>
public interface IValuationService
{
    Task<NetWorthSummary> GetNetWorthAsync(string userId);
    Task<List<AssetValuation>> GetBreakdownAsync(string userId);
}
