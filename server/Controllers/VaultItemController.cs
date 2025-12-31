using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using server.Dtos.VaultItem;
using server.Interfaces;

namespace server.Controllers;

[ApiController]
[Route("api/vault/{vaultId}/items")]
[Authorize]
public class VaultItemController : ControllerBase
{
    private readonly IVaultItemService _itemService;
    private readonly ILogger<VaultItemController> _logger;

    public VaultItemController(IVaultItemService itemService, ILogger<VaultItemController> logger)
    {
        _itemService = itemService;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ??
               User.FindFirstValue("sub") ??
               throw new UnauthorizedAccessException("User ID not found");
    }

    [HttpGet]
    public async Task<ActionResult<List<VaultItemResponseDTO>>> GetVaultItems(int vaultId)
    {
        try
        {
            var userId = GetUserId();
            var items = await _itemService.GetVaultItemsAsync(vaultId, userId);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vault items for vault {VaultId}: {Message}", vaultId, ex.Message);
            return StatusCode(500, new { message = "Failed to retrieve items. Please try again." });
        }
    }

    [HttpGet("{itemId}")]
    public async Task<ActionResult<VaultItemResponseDTO>> GetItem(int vaultId, int itemId)
    {
        try
        {
            var userId = GetUserId();
            var item = await _itemService.GetItemByIdAsync(itemId, userId);
            
            if (item == null)
                return NotFound("Item not found or access denied");

            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item {ItemId} from vault {VaultId}: {Message}", itemId, vaultId, ex.Message);
            return StatusCode(500, new { message = "Failed to retrieve item. Please try again." });
        }
    }

    [HttpPost]
    public async Task<ActionResult<VaultItemResponseDTO>> CreateItem(int vaultId, [FromForm] CreateVaultItemDTO dto)
    {
        try
        {
            // Handle visibilities JSON string from form data
            if (Request.Form.ContainsKey("visibilities"))
            {
                var visibilitiesJson = Request.Form["visibilities"].ToString();
                _logger.LogInformation("Received visibilities JSON for vault {VaultId}: {Json}", vaultId, visibilitiesJson);
                if (!string.IsNullOrEmpty(visibilitiesJson))
                {
                    try
                    {
                        dto.Visibilities = JsonSerializer.Deserialize<List<ItemVisibilityDTO>>(visibilitiesJson);
                        _logger.LogInformation("Deserialized {Count} visibility entries for vault {VaultId}", dto.Visibilities?.Count ?? 0, vaultId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to deserialize visibilities JSON for vault {VaultId}", vaultId);
                    }
                }
            }
            else
            {
                _logger.LogWarning("No visibilities found in form data for vault {VaultId}", vaultId);
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.VaultId != vaultId)
                return BadRequest("Vault ID mismatch");

            var userId = GetUserId();
            var item = await _itemService.CreateItemAsync(dto, userId);
            return CreatedAtAction(nameof(GetItem), new { vaultId, itemId = item.Id }, item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating item in vault {VaultId}: {Message}", vaultId, ex.Message);
            return StatusCode(500, new { message = "Failed to create item. Please try again." });
        }
    }

    [HttpPut("{itemId}")]
    public async Task<ActionResult<VaultItemResponseDTO>> UpdateItem(int vaultId, int itemId, [FromForm] UpdateVaultItemDTO dto)
    {
        try
        {
            // Handle visibilities JSON string from form data
            if (Request.Form.ContainsKey("visibilities"))
            {
                var visibilitiesJson = Request.Form["visibilities"].ToString();
                if (!string.IsNullOrEmpty(visibilitiesJson))
                {
                    dto.Visibilities = JsonSerializer.Deserialize<List<ItemVisibilityDTO>>(visibilitiesJson);
                }
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var item = await _itemService.UpdateItemAsync(itemId, dto, userId);
            
            if (item == null)
                return NotFound("Item not found or insufficient permissions");

            return Ok(item);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item {ItemId} in vault {VaultId}: {Message}", itemId, vaultId, ex.Message);
            return StatusCode(500, new { message = "Failed to update item. Please try again." });
        }
    }

    [HttpDelete("{itemId}")]
    public async Task<ActionResult> DeleteItem(int vaultId, int itemId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _itemService.DeleteItemAsync(itemId, userId);
            
            if (!result)
                return NotFound("Item not found or insufficient permissions");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item {ItemId} from vault {VaultId}: {Message}", itemId, vaultId, ex.Message);
            return StatusCode(500, new { message = "Failed to delete item. Please try again." });
        }
    }

    [HttpPost("{itemId}/restore")]
    public async Task<ActionResult> RestoreItem(int vaultId, int itemId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _itemService.RestoreItemAsync(itemId, userId);
            
            if (!result)
                return BadRequest("Item cannot be restored (not found, not deleted, or recovery window expired)");

            return Ok(new { message = "Item restored successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring item {ItemId} in vault {VaultId}: {Message}", itemId, vaultId, ex.Message);
            return StatusCode(500, new { message = "Failed to restore item. Please try again." });
        }
    }
}