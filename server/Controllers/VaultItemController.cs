using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Dtos.VaultItem;
using server.Interfaces;

namespace server.Controllers;

[ApiController]
[Route("api/vault/{vaultId}/items")]
[Authorize]
public class VaultItemController : ControllerBase
{
    private readonly IVaultItemService _itemService;

    public VaultItemController(IVaultItemService itemService)
    {
        _itemService = itemService;
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
            return StatusCode(500, new { message = "Failed to retrieve items" });
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
            return StatusCode(500, new { message = "Failed to retrieve item" });
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
                // Don't log JSON content in production to avoid exposing sensitive data
                if (!string.IsNullOrEmpty(visibilitiesJson))
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            Converters = { new JsonStringEnumConverter(namingPolicy: null) }
                        };
                        dto.Visibilities = JsonSerializer.Deserialize<List<ItemVisibilityDTO>>(visibilitiesJson, options);
                        // Don't log detailed visibility information in production
                    }
                    catch (Exception ex)
                    {
                        // Failed to deserialize visibilities JSON
                    }
                }
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
            return StatusCode(500, new { message = "Failed to create item" });
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
                // Don't log JSON content in production to avoid exposing sensitive data
                if (!string.IsNullOrEmpty(visibilitiesJson))
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            Converters = { new JsonStringEnumConverter(namingPolicy: null) }
                        };
                        dto.Visibilities = JsonSerializer.Deserialize<List<ItemVisibilityDTO>>(visibilitiesJson, options);
                        // Don't log detailed visibility information in production
                    }
                    catch (Exception ex)
                    {
                        // Failed to deserialize visibilities JSON
                    }
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
            return StatusCode(500, new { message = "Failed to update item" });
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
            return StatusCode(500, new { message = "Failed to delete item" });
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
                return BadRequest("Cannot restore item");

            return Ok(new { message = "Item restored" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to restore item" });
        }
    }
}