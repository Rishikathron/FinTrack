using FinTrack.Interfaces;
using FinTrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;

    // For now, use a hardcoded user ID. Replace with auth in production.
    private const string DefaultUserId = "default-user";

    public AssetsController(IAssetService assetService)
    {
        _assetService = assetService;
    }

    /// <summary>Get all assets for the current user.</summary>
    [HttpGet("list")]
    public async Task<ActionResult<List<Asset>>> GetAllAssets()
    {
        var assets = await _assetService.GetAssetsAsync(DefaultUserId);
        return Ok(assets);
    }

    /// <summary>Get a single asset by ID.</summary>
    [HttpGet("detail/{id:guid}")]
    public async Task<ActionResult<Asset>> GetAssetDetail(Guid id)
    {
        var asset = await _assetService.GetAssetByIdAsync(DefaultUserId, id);
        return asset is not null ? Ok(asset) : NotFound();
    }

    /// <summary>Add a new asset.</summary>
    [HttpPost("add")]
    public async Task<ActionResult<Asset>> CreateAsset([FromBody] AddAssetRequest request)
    {
        var asset = await _assetService.AddAssetAsync(DefaultUserId, request);
        return CreatedAtAction(nameof(GetAssetDetail), new { id = asset.Id }, asset);
    }

    /// <summary>Update an existing asset (quantity for Gold/Silver, amount for FD).</summary>
    [HttpPut("edit/{id:guid}")]
    public async Task<ActionResult<Asset>> EditAsset(Guid id, [FromBody] UpdateAssetRequest request)
    {
        var updated = await _assetService.UpdateAssetAsync(DefaultUserId, id, request);
        return updated is not null ? Ok(updated) : NotFound();
    }

    /// <summary>Delete an asset by ID.</summary>
    [HttpDelete("remove/{id:guid}")]
    public async Task<IActionResult> RemoveAsset(Guid id)
    {
        var deleted = await _assetService.DeleteAssetAsync(DefaultUserId, id);
        return deleted ? NoContent() : NotFound();
    }
}
