using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using server.Dtos.Vault;
using server.Interfaces;
using server.Models;

namespace server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VaultController : ControllerBase
{
    private readonly IVaultService _vaultService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<VaultController> _logger;

    public VaultController(IVaultService vaultService, UserManager<User> userManager, ILogger<VaultController> logger)
    {
        _vaultService = vaultService;
        _userManager = userManager;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ??
               User.FindFirstValue("sub") ??
               throw new UnauthorizedAccessException("User ID not found");
    }

    // Vault CRUD operations
    [HttpGet]
    public async Task<ActionResult<List<VaultResponseDTO>>> GetUserVaults()
    {
        var userId = GetUserId();
        var vaults = await _vaultService.GetUserVaultsAsync(userId);
        return Ok(vaults);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VaultResponseDTO>> GetVault(int id)
    {
        var userId = GetUserId();
        var vault = await _vaultService.GetVaultByIdAsync(id, userId);
        
        if (vault == null)
            return NotFound("Vault not found or access denied");

        return Ok(vault);
    }

    [HttpPost]
    public async Task<ActionResult<VaultResponseDTO>> CreateVault([FromBody] CreateVaultDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        var vault = await _vaultService.CreateVaultAsync(dto, userId);
        return CreatedAtAction(nameof(GetVault), new { id = vault.Id }, vault);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<VaultResponseDTO>> UpdateVault(int id, [FromBody] UpdateVaultDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        var vault = await _vaultService.UpdateVaultAsync(id, dto, userId);
        
        if (vault == null)
            return NotFound("Vault not found or insufficient permissions");

        return Ok(vault);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteVault(int id)
    {
        var userId = GetUserId();
        var result = await _vaultService.DeleteVaultAsync(id, userId);
        
        if (!result)
            return NotFound("Vault not found or insufficient permissions");

        return NoContent();
    }

    [HttpPost("{id}/restore")]
    public async Task<ActionResult> RestoreVault(int id)
    {
        var userId = GetUserId();
        var result = await _vaultService.RestoreVaultAsync(id, userId);
        
        if (!result)
            return BadRequest("Vault cannot be restored");

        return Ok(new { message = "Vault restored successfully" });
    }

    // Invite operations
    [HttpPost("{id}/invites")]
    public async Task<ActionResult<VaultInviteResponseDTO>> CreateInvite(int id, [FromBody] CreateInviteDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        try
        {
            var invite = await _vaultService.CreateInviteAsync(id, dto, userId);
            return CreatedAtAction(nameof(GetInvite), new { id = id, inviteId = invite.Id }, invite);
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
            // Log the exception for debugging
            _logger.LogError(ex, "Error creating invite for vault {VaultId}: {Message}", id, ex.Message);
            return StatusCode(500, new { message = "Failed to create invite" });
        }
    }

    [HttpGet("{id}/invites")]
    public async Task<ActionResult<List<VaultInviteResponseDTO>>> GetVaultInvites(int id)
    {
        var userId = GetUserId();
        var invites = await _vaultService.GetVaultInvitesAsync(id, userId);
        return Ok(invites);
    }

    [HttpGet("{id}/invites/{inviteId}")]
    public async Task<ActionResult<VaultInviteResponseDTO>> GetInvite(int id, int inviteId)
    {
        var userId = GetUserId();
        var invites = await _vaultService.GetVaultInvitesAsync(id, userId);
        var invite = invites.FirstOrDefault(i => i.Id == inviteId);
        
        if (invite == null)
            return NotFound("Invite not found or access denied");

        return Ok(invite);
    }

    [HttpPost("{id}/invites/{inviteId}/cancel")]
    public async Task<ActionResult> CancelInvite(int id, int inviteId)
    {
        var userId = GetUserId();
        var result = await _vaultService.CancelInviteAsync(inviteId, userId);
        
        if (!result)
            return BadRequest("Cannot cancel invite");

        return Ok(new { message = "Invite cancelled successfully" });
    }

    [HttpPost("{id}/invites/{inviteId}/resend")]
    public async Task<ActionResult> ResendInvite(int id, int inviteId)
    {
        var userId = GetUserId();
        var result = await _vaultService.ResendInviteAsync(inviteId, userId);
        
        if (!result)
            return BadRequest("Cannot resend invite");

        return Ok(new { message = "Invite resent successfully" });
    }

    [HttpGet("invites/info")]
    [AllowAnonymous]
    public async Task<ActionResult<InviteInfoDTO>> GetInviteInfo([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
            return BadRequest("Token is required");

        var info = await _vaultService.GetInviteInfoAsync(token);
        
        // Check if user exists for this email
        if (info.IsValid && !string.IsNullOrEmpty(info.InviteeEmail))
        {
            var user = await _userManager.FindByEmailAsync(info.InviteeEmail);
            info.UserExists = user != null;
        }
        
        return Ok(info);
    }

    [HttpPost("invites/accept")]
    public async Task<ActionResult> AcceptInvite([FromBody] AcceptInviteDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        var result = await _vaultService.AcceptInviteAsync(dto.Token, dto.Email, userId);
        
        if (!result)
            return BadRequest("Invalid invite");

        return Ok(new { message = "Invite accepted successfully" });
    }

    // Member operations
    [HttpGet("{id}/members")]
    public async Task<ActionResult<List<VaultMemberResponseDTO>>> GetVaultMembers(int id)
    {
        var userId = GetUserId();
        var members = await _vaultService.GetVaultMembersAsync(id, userId);
        return Ok(members);
    }

    [HttpDelete("{id}/members/{memberId}")]
    public async Task<ActionResult> RemoveMember(int id, int memberId)
    {
        var userId = GetUserId();
        var result = await _vaultService.RemoveMemberAsync(id, memberId, userId);
        
        if (!result)
            return BadRequest("Cannot remove member");

        return NoContent();
    }

    [HttpPut("{id}/members/privilege")]
    public async Task<ActionResult> UpdateMemberPrivilege(int id, [FromBody] UpdateMemberPrivilegeDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        var result = await _vaultService.UpdateMemberPrivilegeAsync(id, dto, userId);
        
        if (!result)
            return BadRequest("Cannot update privilege");

        return Ok(new { message = "Member privilege updated successfully" });
    }

    [HttpPost("{id}/transfer-ownership")]
    public async Task<ActionResult> TransferOwnership(int id, [FromBody] TransferOwnershipDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();
        var result = await _vaultService.TransferOwnershipAsync(id, dto, userId);
        
        if (!result)
            return BadRequest("Cannot transfer ownership");

        return Ok(new { message = "Ownership transferred successfully" });
    }

    [HttpPost("{id}/leave")]
    public async Task<ActionResult> LeaveVault(int id)
    {
        var userId = GetUserId();
        var result = await _vaultService.LeaveVaultAsync(id, userId);
        
        if (!result)
            return BadRequest("Cannot leave vault");

        return Ok(new { message = "Left vault successfully" });
    }

    // Policy operations
    [HttpPost("{id}/release")]
    public async Task<ActionResult> ReleaseVaultManually(int id)
    {
        var userId = GetUserId();
        var result = await _vaultService.ReleaseVaultManuallyAsync(id, userId);
        
        if (!result)
            return BadRequest("Cannot release vault");

        return Ok(new { message = "Vault released successfully" });
    }

    // Logs
    [HttpGet("{id}/logs")]
    public async Task<ActionResult<List<VaultLogResponseDTO>>> GetVaultLogs(int id)
    {
        try
        {
            var userId = GetUserId();
            var logs = await _vaultService.GetVaultLogsAsync(id, userId);
            return Ok(logs);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vault logs for vault {VaultId}: {Message}", id, ex.Message);
            return StatusCode(500, new { message = "Failed to retrieve vault logs" });
        }
    }
}

