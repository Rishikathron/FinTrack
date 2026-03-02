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
    [HttpGet]
    public async Task<ActionResult<List<Asset>>> GetAssets()
    {
        var assets = await _assetService.GetAssetsAsync(DefaultUserId);
        return Ok(assets);
    }

    /// <summary>Add a new asset.</summary>
    [HttpPost]
    public async Task<ActionResult<Asset>> AddAsset([FromBody] AddAssetRequest request)
    {
        var asset = await _assetService.AddAssetAsync(DefaultUserId, request);
        return CreatedAtAction(nameof(GetAssets), new { id = asset.Id }, asset);
    }

    /// <summary>Delete an asset by ID.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsset(Guid id)
    {
        var deleted = await _assetService.DeleteAssetAsync(DefaultUserId, id);
        return deleted ? NoContent() : NotFound();
    }
}
