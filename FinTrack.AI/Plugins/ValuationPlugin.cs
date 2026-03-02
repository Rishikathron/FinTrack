using System.ComponentModel;
using System.Text.Json;
using FinTrack.Interfaces;
using Microsoft.SemanticKernel;

namespace FinTrack.AI.Plugins;

/// <summary>
/// Semantic Kernel plugin for valuation and net worth calculations.
/// Wraps IValuationService so the AI agent can query portfolio insights.
/// </summary>
public class ValuationPlugin
{
    private readonly IValuationService _valuationService;
    private const string UserId = "default-user";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ValuationPlugin(IValuationService valuationService)
    {
        _valuationService = valuationService;
    }

    [KernelFunction("get_net_worth")]
    [Description("Get total net worth summary including gold value, silver value, FD value, total invested, profit/loss breakdown by category, and bank-wise FD split. Returns JSON.")]
    public async Task<string> GetNetWorthAsync()
    {
        var summary = await _valuationService.GetNetWorthAsync(UserId);
        return JsonSerializer.Serialize(summary, _jsonOptions);
    }

    [KernelFunction("get_valuation_breakdown")]
    [Description("Get per-asset valuation breakdown showing each asset's current market value, purchase value, profit/loss, and FD-specific details like accrued interest and maturity date. Returns JSON array.")]
    public async Task<string> GetBreakdownAsync()
    {
        var breakdown = await _valuationService.GetBreakdownAsync(UserId);
        return JsonSerializer.Serialize(breakdown, _jsonOptions);
    }
}
