using FinTrack.Interfaces;
using FinTrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PricesController : ControllerBase
{
    private readonly IPriceProvider _priceProvider;

    public PricesController(IPriceProvider priceProvider)
    {
        _priceProvider = priceProvider;
    }

    /// <summary>Get current gold and silver prices per gram in INR.</summary>
    [HttpGet("current")]
    public async Task<ActionResult<MetalPrices>> GetCurrentPrices()
    {
        var prices = await _priceProvider.GetCurrentPricesAsync();
        return Ok(prices);
    }
}
