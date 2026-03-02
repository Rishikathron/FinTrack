using FinTrack.Interfaces;
using FinTrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ValuationController : ControllerBase
{
    private readonly IValuationService _valuationService;
    private const string DefaultUserId = "default-user";

    public ValuationController(IValuationService valuationService)
    {
        _valuationService = valuationService;
    }

    /// <summary>Get total net worth summary.</summary>
    [HttpGet("networth")]
    public async Task<ActionResult<NetWorthSummary>> GetNetWorth()
    {
        var summary = await _valuationService.GetNetWorthAsync(DefaultUserId);
        return Ok(summary);
    }

    /// <summary>Get per-asset valuation breakdown.</summary>
    [HttpGet("breakdown")]
    public async Task<ActionResult<List<AssetValuation>>> GetBreakdown()
    {
        var breakdown = await _valuationService.GetBreakdownAsync(DefaultUserId);
        return Ok(breakdown);
    }
}
