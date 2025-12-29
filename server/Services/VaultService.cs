using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using server.Dtos.Vault;
using server.Interfaces;
using server.Models;

namespace server.Services;

public class VaultService : IVaultService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<VaultService> _logger;

    public VaultService(
        ApplicationDBContext dbContext,
        UserManager<User> userManager,
        IEmailService emailService,
        IConfiguration config,
        ILogger<VaultService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _emailService = emailService;
        _config = config;
        _logger = logger;
    }

    public async Task<VaultResponseDTO?> GetVaultByIdAsync(int vaultId, string userId)
    {
        var vault = await _dbContext.Vaults
            .Include(v => v.Owner)
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null)
            return null;

        // Check if user has access
        var privilege = await GetUserPrivilegeAsync(vaultId, userId);
        if (privilege == null && vault.OwnerId != userId)
            return null;

        // Don't return deleted vaults unless user is owner
        if (vault.Status == VaultStatus.Deleted && vault.OwnerId != userId)
            return null;

        // Get original owner info
        var originalOwner = await _userManager.FindByIdAsync(vault.OriginalOwnerId);

        return new VaultResponseDTO
        {
            Id = vault.Id,
            OwnerId = vault.OwnerId,
            OwnerEmail = vault.Owner?.Email,
            OwnerName = vault.Owner?.UserName,
            OriginalOwnerId = vault.OriginalOwnerId,
            OriginalOwnerEmail = originalOwner?.Email,
            OriginalOwnerName = originalOwner?.UserName,
            Name = vault.Name,
            Description = vault.Description,
            Status = vault.Status,
            CreatedAt = vault.CreatedAt,
            DeletedAt = vault.DeletedAt,
            UserPrivilege = privilege ?? (vault.OwnerId == userId ? Privilege.Owner : null)
        };
    }

    public async Task<List<VaultResponseDTO>> GetUserVaultsAsync(string userId)
    {
        var vaults = await _dbContext.Vaults
            .Include(v => v.Members)
            .Where(v => v.Status == VaultStatus.Active && 
                       (v.OwnerId == userId || 
                        v.Members.Any(m => m.UserId == userId && m.Status == MemberStatus.Active)))
            .ToListAsync();

        var result = new List<VaultResponseDTO>();
        foreach (var vault in vaults)
        {
            var privilege = await GetUserPrivilegeAsync(vault.Id, userId);
            result.Add(new VaultResponseDTO
            {
                Id = vault.Id,
                OwnerId = vault.OwnerId,
                OriginalOwnerId = vault.OriginalOwnerId,
                Name = vault.Name,
                Description = vault.Description,
                Status = vault.Status,
                CreatedAt = vault.CreatedAt,
                DeletedAt = vault.DeletedAt,
                UserPrivilege = privilege ?? (vault.OwnerId == userId ? Privilege.Owner : null)
            });
        }

        return result;
    }

    public async Task<VaultResponseDTO> CreateVaultAsync(CreateVaultDTO dto, string userId)
    {
        var vault = new Vault
        {
            OwnerId = userId,
            OriginalOwnerId = userId,
            Name = dto.Name,
            Description = dto.Description,
            Status = VaultStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Vaults.Add(vault);
        await _dbContext.SaveChangesAsync();

        // Create owner member record
        var member = new VaultMember
        {
            VaultId = vault.Id,
            UserId = userId,
            Privilege = Privilege.Owner,
            Status = MemberStatus.Active,
            AddedById = userId,
            JoinedAt = DateTime.UtcNow
        };

        _dbContext.VaultMembers.Add(member);
        await _dbContext.SaveChangesAsync();

        return new VaultResponseDTO
        {
            Id = vault.Id,
            OwnerId = vault.OwnerId,
            OriginalOwnerId = vault.OriginalOwnerId,
            Name = vault.Name,
            Description = vault.Description,
            Status = vault.Status,
            CreatedAt = vault.CreatedAt,
            DeletedAt = vault.DeletedAt,
            UserPrivilege = Privilege.Owner
        };
    }

    public async Task<VaultResponseDTO?> UpdateVaultAsync(int vaultId, UpdateVaultDTO dto, string userId)
    {
        var vault = await _dbContext.Vaults
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null || vault.Status == VaultStatus.Deleted)
            return null;

        // Check permissions: Owner or Admin can edit
        var privilege = await GetUserPrivilegeAsync(vaultId, userId);
        if (privilege != Privilege.Owner && privilege != Privilege.Admin)
            return null;

        vault.Name = dto.Name;
        vault.Description = dto.Description;

        await _dbContext.SaveChangesAsync();

        return new VaultResponseDTO
        {
            Id = vault.Id,
            OwnerId = vault.OwnerId,
            OriginalOwnerId = vault.OriginalOwnerId,
            Name = vault.Name,
            Description = vault.Description,
            Status = vault.Status,
            CreatedAt = vault.CreatedAt,
            DeletedAt = vault.DeletedAt,
            UserPrivilege = privilege ?? Privilege.Owner
        };
    }

    public async Task<bool> DeleteVaultAsync(int vaultId, string userId)
    {
        var vault = await _dbContext.Vaults
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null || vault.Status == VaultStatus.Deleted)
            return false;

        // Only owner can delete
        if (vault.OwnerId != userId)
            return false;

        vault.Status = VaultStatus.Deleted;
        vault.DeletedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreVaultAsync(int vaultId, string userId)
    {
        var vault = await _dbContext.Vaults
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null || vault.Status != VaultStatus.Deleted)
            return false;

        // Only owner can restore
        if (vault.OwnerId != userId)
            return false;

        // Check if within 30 days recovery window
        if (vault.DeletedAt.HasValue && (DateTime.UtcNow - vault.DeletedAt.Value).TotalDays > 30)
            return false;

        vault.Status = VaultStatus.Active;
        vault.DeletedAt = null;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<VaultInviteResponseDTO> CreateInviteAsync(int vaultId, CreateInviteDTO dto, string inviterId)
    {
        var vault = await _dbContext.Vaults
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null || vault.Status == VaultStatus.Deleted)
            throw new InvalidOperationException("Vault not found");

        // Check permissions: Owner or Admin can invite
        var privilege = await GetUserPrivilegeAsync(vaultId, inviterId);
        if (privilege != Privilege.Owner && privilege != Privilege.Admin)
            throw new UnauthorizedAccessException("Insufficient permissions");

        // Admins cannot invite as Owner
        if (privilege == Privilege.Admin && dto.Privilege == Privilege.Owner)
            throw new UnauthorizedAccessException("Admins cannot invite as Owner");

        // Check if user is already an active member (allow re-inviting users who left or were removed)
        var inviteeUser = await _userManager.FindByEmailAsync(dto.InviteeEmail);
        var existingActiveMember = inviteeUser != null ? await _dbContext.VaultMembers
            .FirstOrDefaultAsync(m => m.VaultId == vaultId && 
                                     m.UserId == inviteeUser.Id &&
                                     m.Status == MemberStatus.Active) : null;

        if (existingActiveMember != null)
            throw new InvalidOperationException("User is already a member");

        // Generate token
        var token = GenerateInviteToken();
        var tokenHash = HashToken(token);

        var invite = new VaultInvite
        {
            VaultId = vaultId,
            InviterId = inviterId,
            InviteeEmail = dto.InviteeEmail,
            InviteeId = (await _userManager.FindByEmailAsync(dto.InviteeEmail))?.Id,
            Privilege = dto.Privilege,
            InviteType = dto.InviteType,
            Status = InviteStatus.Pending,
            ExpiresAt = dto.ExpiresAt ?? DateTime.UtcNow.AddDays(7),
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            Note = dto.Note
        };

        _dbContext.VaultInvites.Add(invite);
        await _dbContext.SaveChangesAsync();

        // Get inviter info before sending email
        var inviter = await _userManager.FindByIdAsync(inviterId);
        
        // Send email if immediate invite
        // Note: Making this synchronous for now to ensure emails are sent and errors are caught
        if (dto.InviteType == InviteType.Immediate)
        {
            try
            {
                await SendInviteEmailAsync(invite, token);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the invite creation
                _logger.LogError(ex, "Failed to send invite email for invite {InviteId}, but invite was created", invite.Id);
                // Reload invite to get latest status
                await _dbContext.Entry(invite).ReloadAsync();
            }
        }
        
        return new VaultInviteResponseDTO
        {
            Id = invite.Id,
            VaultId = invite.VaultId,
            InviterId = invite.InviterId,
            InviterEmail = inviter?.Email,
            InviteeEmail = invite.InviteeEmail,
            InviteeId = invite.InviteeId,
            Privilege = invite.Privilege,
            InviteType = invite.InviteType,
            Status = invite.Status,
            SentAt = invite.SentAt,
            ExpiresAt = invite.ExpiresAt,
            CreatedAt = invite.CreatedAt,
            AcceptedAt = invite.AcceptedAt,
            Note = invite.Note
        };
    }

    public async Task<List<VaultInviteResponseDTO>> GetVaultInvitesAsync(int vaultId, string userId)
    {
        var vault = await _dbContext.Vaults
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null)
            return new List<VaultInviteResponseDTO>();

        // Check permissions: Owner or Admin can view invites
        var privilege = await GetUserPrivilegeAsync(vaultId, userId);
        if (privilege != Privilege.Owner && privilege != Privilege.Admin)
            return new List<VaultInviteResponseDTO>();

        var invites = await _dbContext.VaultInvites
            .Where(i => i.VaultId == vaultId)
            .Include(i => i.Inviter)
            .ToListAsync();

        return invites.Select(i => new VaultInviteResponseDTO
        {
            Id = i.Id,
            VaultId = i.VaultId,
            InviterId = i.InviterId,
            InviterEmail = i.Inviter?.Email,
            InviteeEmail = i.InviteeEmail,
            InviteeId = i.InviteeId,
            Privilege = i.Privilege,
            InviteType = i.InviteType,
            Status = i.Status,
            SentAt = i.SentAt,
            ExpiresAt = i.ExpiresAt,
            CreatedAt = i.CreatedAt,
            AcceptedAt = i.AcceptedAt,
            Note = i.Note
        }).ToList();
    }

    public async Task<bool> CancelInviteAsync(int inviteId, string userId)
    {
        var invite = await _dbContext.VaultInvites
            .Include(i => i.Vault)
            .FirstOrDefaultAsync(i => i.Id == inviteId);

        if (invite == null || invite.Status == InviteStatus.Accepted || invite.Status == InviteStatus.Cancelled)
            return false;

        var privilege = await GetUserPrivilegeAsync(invite.VaultId, userId);
        
        // Owner can cancel any invite, Admin can only cancel their own
        if (privilege != Privilege.Owner && (privilege != Privilege.Admin || invite.InviterId != userId))
            return false;

        invite.Status = InviteStatus.Cancelled;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResendInviteAsync(int inviteId, string userId)
    {
        var invite = await _dbContext.VaultInvites
            .Include(i => i.Vault)
            .FirstOrDefaultAsync(i => i.Id == inviteId);

        if (invite == null || invite.Status == InviteStatus.Accepted || invite.Status == InviteStatus.Cancelled)
            return false;

        var privilege = await GetUserPrivilegeAsync(invite.VaultId, userId);
        
        // Owner can resend any invite, Admin can only resend their own
        if (privilege != Privilege.Owner && (privilege != Privilege.Admin || invite.InviterId != userId))
            return false;

        // Generate new token
        var token = GenerateInviteToken();
        invite.TokenHash = HashToken(token);
        invite.Status = InviteStatus.Pending;
        invite.SentAt = null;

        await _dbContext.SaveChangesAsync();

        // Send email
        await SendInviteEmailAsync(invite, token);
        return true;
    }

    public async Task<InviteInfoDTO> GetInviteInfoAsync(string token)
    {
        var tokenHash = HashToken(token);
        var invite = await _dbContext.VaultInvites
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash);

        if (invite == null)
        {
            return new InviteInfoDTO
            {
                IsValid = false,
                ErrorMessage = "Invalid invite token"
            };
        }

        // Check if expired
        if (invite.ExpiresAt.HasValue && invite.ExpiresAt.Value < DateTime.UtcNow)
        {
            invite.Status = InviteStatus.Expired;
            await _dbContext.SaveChangesAsync();
            return new InviteInfoDTO
            {
                IsValid = false,
                ErrorMessage = "Invite has expired"
            };
        }

        // Check if already accepted or cancelled
        if (invite.Status == InviteStatus.Accepted || invite.Status == InviteStatus.Cancelled)
        {
            return new InviteInfoDTO
            {
                IsValid = false,
                ErrorMessage = invite.Status == InviteStatus.Accepted ? "Invite has already been accepted" : "Invite has been cancelled"
            };
        }

        return new InviteInfoDTO
        {
            InviteeEmail = invite.InviteeEmail,
            IsValid = true
        };
    }

    public async Task<bool> AcceptInviteAsync(string token, string email, string userId)
    {
        var tokenHash = HashToken(token);
        var invite = await _dbContext.VaultInvites
            .Include(i => i.Vault)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash);

        if (invite == null)
            return false;

        // Check if expired
        if (invite.ExpiresAt.HasValue && invite.ExpiresAt.Value < DateTime.UtcNow)
        {
            invite.Status = InviteStatus.Expired;
            await _dbContext.SaveChangesAsync();
            return false;
        }

        // Check if already accepted or cancelled
        if (invite.Status == InviteStatus.Accepted || invite.Status == InviteStatus.Cancelled)
            return false;

        // Verify email matches invite
        if (email != invite.InviteeEmail)
            return false;

        // Verify email matches user
        var user = await _userManager.FindByIdAsync(userId);
        if (user?.Email != email)
            return false;

        // Check if already an active member
        var existingActiveMember = await _dbContext.VaultMembers
            .FirstOrDefaultAsync(m => m.VaultId == invite.VaultId && 
                                     m.UserId == userId && 
                                     m.Status == MemberStatus.Active);

        if (existingActiveMember != null)
        {
            invite.Status = InviteStatus.Accepted;
            invite.AcceptedAt = DateTime.UtcNow;
            invite.InviteeId = userId;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // Check if member previously left or was removed - reactivate them
        // First check ALL members (not just active) to find any existing record
        var anyExistingMember = await _dbContext.VaultMembers
            .FirstOrDefaultAsync(m => m.VaultId == invite.VaultId && m.UserId == userId);

        if (anyExistingMember != null)
        {
            _logger.LogInformation("Found existing member record {MemberId} for user {UserId} in vault {VaultId}. Current status: {Status}", 
                anyExistingMember.Id, userId, invite.VaultId, anyExistingMember.Status);
            
            if (anyExistingMember.Status == MemberStatus.Active)
            {
                // Already active, just mark invite as accepted
                _logger.LogInformation("Member {UserId} is already active in vault {VaultId}", userId, invite.VaultId);
            }
            else
            {
                // Reactivate the member
                _logger.LogInformation("Reactivating member {UserId} for vault {VaultId}. Previous status: {Status}", 
                    userId, invite.VaultId, anyExistingMember.Status);
                
                anyExistingMember.Status = MemberStatus.Active;
                anyExistingMember.Privilege = invite.Privilege;
                anyExistingMember.AddedById = invite.InviterId;
                anyExistingMember.JoinedAt = DateTime.UtcNow; // Update join date to now
                anyExistingMember.LeftAt = null;
                anyExistingMember.RemovedAt = null;
                anyExistingMember.RemovedById = null;
                
                // Use Update to ensure EF tracks all changes
                _dbContext.VaultMembers.Update(anyExistingMember);
                
                _logger.LogInformation("Member {UserId} reactivated for vault {VaultId}. New status: {Status}", 
                    userId, invite.VaultId, anyExistingMember.Status);
            }
        }
        else
        {
            _logger.LogInformation("No existing member record found. Creating new member record for {UserId} in vault {VaultId}", userId, invite.VaultId);
            
            // Create new member record
            var member = new VaultMember
            {
                VaultId = invite.VaultId,
                UserId = userId,
                Privilege = invite.Privilege,
                Status = MemberStatus.Active,
                AddedById = invite.InviterId,
                JoinedAt = DateTime.UtcNow
            };

            _dbContext.VaultMembers.Add(member);
        }

        // Update invite
        invite.Status = InviteStatus.Accepted;
        invite.AcceptedAt = DateTime.UtcNow;
        invite.InviteeId = userId;

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Successfully saved member changes for {UserId} in vault {VaultId}", userId, invite.VaultId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save member changes for {UserId} in vault {VaultId}", userId, invite.VaultId);
            throw;
        }
    }

    public async Task<List<VaultMemberResponseDTO>> GetVaultMembersAsync(int vaultId, string userId)
    {
        // Check if user has access to vault
        var privilege = await GetUserPrivilegeAsync(vaultId, userId);
        if (privilege == null)
        {
            var vault = await _dbContext.Vaults.FirstOrDefaultAsync(v => v.Id == vaultId);
            if (vault?.OwnerId != userId)
                return new List<VaultMemberResponseDTO>();
        }

        var members = await _dbContext.VaultMembers
            .Where(m => m.VaultId == vaultId)
            .Include(m => m.User)
            .Include(m => m.AddedBy)
            .Include(m => m.RemovedBy)
            .ToListAsync();

        return members.Select(m => new VaultMemberResponseDTO
        {
            Id = m.Id,
            VaultId = m.VaultId,
            UserId = m.UserId,
            UserEmail = m.User?.Email,
            UserName = m.User?.UserName,
            Privilege = m.Privilege,
            Status = m.Status,
            RemovedById = m.RemovedById,
            RemovedByEmail = m.RemovedBy?.Email,
            RemovedByName = m.RemovedBy?.UserName,
            AddedById = m.AddedById,
            AddedByEmail = m.AddedBy?.Email,
            AddedByName = m.AddedBy?.UserName,
            JoinedAt = m.JoinedAt,
            LeftAt = m.LeftAt,
            RemovedAt = m.RemovedAt
        }).ToList();
    }

    public async Task<bool> RemoveMemberAsync(int vaultId, int memberId, string userId)
    {
        var member = await _dbContext.VaultMembers
            .Include(m => m.Vault)
            .FirstOrDefaultAsync(m => m.Id == memberId && m.VaultId == vaultId);

        if (member == null || member.Status != MemberStatus.Active)
            return false;

        var privilege = await GetUserPrivilegeAsync(vaultId, userId);

        // Owner can remove anyone (except themselves without transfer)
        if (privilege == Privilege.Owner)
        {
            // Owner cannot remove themselves
            if (member.UserId == userId)
                return false;

            member.Status = MemberStatus.Removed;
            member.RemovedById = userId;
            member.RemovedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // Admin can only remove Members
        if (privilege == Privilege.Admin && member.Privilege == Privilege.Member)
        {
            member.Status = MemberStatus.Removed;
            member.RemovedById = userId;
            member.RemovedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> UpdateMemberPrivilegeAsync(int vaultId, UpdateMemberPrivilegeDTO dto, string userId)
    {
        var member = await _dbContext.VaultMembers
            .Include(m => m.Vault)
            .FirstOrDefaultAsync(m => m.Id == dto.MemberId && m.VaultId == vaultId);

        if (member == null || member.Status != MemberStatus.Active)
            return false;

        var privilege = await GetUserPrivilegeAsync(vaultId, userId);

        // Owner can change any member's privilege
        if (privilege == Privilege.Owner)
        {
            // Cannot change owner privilege (must use transfer ownership)
            if (member.Privilege == Privilege.Owner && dto.Privilege != Privilege.Owner)
                return false;

            // If promoting to Owner, must use transfer ownership
            if (dto.Privilege == Privilege.Owner)
                return false;

            member.Privilege = dto.Privilege;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // Admin can change Member and Admin privileges (Member ↔ Admin, but not Owner)
        // Admin can promote Members to Admin, or demote Admins to Member
        if (privilege == Privilege.Admin)
        {
            // Cannot change Owner privileges
            if (member.Privilege == Privilege.Owner)
                return false;
            
            // Cannot promote to Owner
            if (dto.Privilege == Privilege.Owner)
                return false;
            
            // Can change Member to Admin or keep as Member
            if (member.Privilege == Privilege.Member && 
                (dto.Privilege == Privilege.Member || dto.Privilege == Privilege.Admin))
            {
                member.Privilege = dto.Privilege;
                await _dbContext.SaveChangesAsync();
                return true;
            }
            
            // Can demote Admin to Member
            if (member.Privilege == Privilege.Admin && dto.Privilege == Privilege.Member)
            {
                member.Privilege = dto.Privilege;
                await _dbContext.SaveChangesAsync();
                return true;
            }
        }

        return false;
    }

    public async Task<bool> TransferOwnershipAsync(int vaultId, TransferOwnershipDTO dto, string userId)
    {
        var vault = await _dbContext.Vaults
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null || vault.OwnerId != userId)
            return false;

        var newOwner = await _dbContext.VaultMembers
            .FirstOrDefaultAsync(m => m.Id == dto.MemberId && 
                                     m.VaultId == vaultId && 
                                     m.Status == MemberStatus.Active);

        if (newOwner == null || newOwner.Privilege != Privilege.Admin)
            return false;

        // Update vault owner
        vault.OwnerId = newOwner.UserId;

        // Update member privileges
        var oldOwnerMember = await _dbContext.VaultMembers
            .FirstOrDefaultAsync(m => m.VaultId == vaultId && m.UserId == userId && m.Status == MemberStatus.Active);
        
        if (oldOwnerMember != null)
        {
            oldOwnerMember.Privilege = Privilege.Admin;
        }

        newOwner.Privilege = Privilege.Owner;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LeaveVaultAsync(int vaultId, string userId)
    {
        var member = await _dbContext.VaultMembers
            .Include(m => m.Vault)
            .FirstOrDefaultAsync(m => m.VaultId == vaultId && m.UserId == userId && m.Status == MemberStatus.Active);

        if (member == null)
            return false;

        // Owner cannot leave without transferring ownership
        if (member.Privilege == Privilege.Owner)
            return false;

        member.Status = MemberStatus.Left;
        member.LeftAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    // Permission check methods
    public async Task<Privilege?> GetUserPrivilegeAsync(int vaultId, string userId)
    {
        var vault = await _dbContext.Vaults.FirstOrDefaultAsync(v => v.Id == vaultId);
        if (vault == null)
            return null;

        // Owner
        if (vault.OwnerId == userId)
            return Privilege.Owner;

        // Check member
        var member = await _dbContext.VaultMembers
            .FirstOrDefaultAsync(m => m.VaultId == vaultId && 
                                     m.UserId == userId && 
                                     m.Status == MemberStatus.Active);

        return member?.Privilege;
    }

    public async Task<bool> CanViewVaultAsync(int vaultId, string userId)
    {
        var privilege = await GetUserPrivilegeAsync(vaultId, userId);
        return privilege != null;
    }

    public async Task<bool> CanEditVaultAsync(int vaultId, string userId)
    {
        var privilege = await GetUserPrivilegeAsync(vaultId, userId);
        return privilege == Privilege.Owner || privilege == Privilege.Admin;
    }

    public async Task<bool> CanManageMembersAsync(int vaultId, string userId)
    {
        var privilege = await GetUserPrivilegeAsync(vaultId, userId);
        return privilege == Privilege.Owner || privilege == Privilege.Admin;
    }

    public async Task<bool> CanDeleteVaultAsync(int vaultId, string userId)
    {
        var vault = await _dbContext.Vaults.FirstOrDefaultAsync(v => v.Id == vaultId);
        return vault?.OwnerId == userId;
    }

    // Helper methods
    private string GenerateInviteToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }

    private async Task SendInviteEmailAsync(VaultInvite invite, string token)
    {
        try
        {
            var vault = await _dbContext.Vaults.FirstOrDefaultAsync(v => v.Id == invite.VaultId);
            var inviter = await _userManager.FindByIdAsync(invite.InviterId);
            
            if (vault == null || inviter == null)
            {
                _logger.LogWarning("Cannot send invite email: Vault {VaultId} or inviter {InviterId} not found", invite.VaultId, invite.InviterId);
                return;
            }

            var baseUrl = _config["App:BaseUrl"];
            var inviteUrl = $"{baseUrl}/accept-invite?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(invite.InviteeEmail)}";

            var privilegeName = invite.Privilege switch
            {
                Privilege.Owner => "Owner",
                Privilege.Admin => "Admin",
                Privilege.Member => "Member",
                _ => invite.Privilege.ToString()
            };

            _logger.LogInformation("Attempting to send vault invite email to {Email} for vault {VaultId}", invite.InviteeEmail, invite.VaultId);

            await _emailService.SendVaultInviteAsync(
                invite.InviteeEmail,
                inviter.UserName ?? inviter.Email ?? "Someone",
                vault.Name,
                inviteUrl,
                privilegeName,
                invite.Note
            );
            
            _logger.LogInformation("Successfully sent vault invite email to {Email} for vault {VaultId}", invite.InviteeEmail, invite.VaultId);
            
            // Update invite status to Sent
            invite.Status = InviteStatus.Sent;
            invite.SentAt = DateTime.UtcNow;
            
            // Save changes - wrap in try-catch in case of concurrency issues
            try
            {
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Updated invite {InviteId} status to Sent", invite.Id);
            }
            catch (Exception saveEx)
            {
                _logger.LogWarning(saveEx, "Failed to save invite status update for invite {InviteId}", invite.Id);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail - invite is already created
            // The invite can be resent later
            _logger.LogError(ex, "Failed to send vault invite email to {Email} for vault {VaultId}. Error: {ErrorMessage}", 
                invite.InviteeEmail, invite.VaultId, ex.Message);
            
            // Try to update status to Pending if email fails
            try
            {
                invite.Status = InviteStatus.Pending;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Updated invite {InviteId} status to Pending after email failure", invite.Id);
            }
            catch (Exception saveEx)
            {
                _logger.LogWarning(saveEx, "Failed to update invite {InviteId} status to Pending", invite.Id);
            }
        }
    }
}

