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
    private readonly IS3Service _s3Service;
    private readonly INotificationService _notificationService;

    public VaultService(
        ApplicationDBContext dbContext,
        UserManager<User> userManager,
        IEmailService emailService,
        IConfiguration config,
        ILogger<VaultService> logger,
        IS3Service s3Service,
        INotificationService notificationService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _emailService = emailService;
        _config = config;
        _logger = logger;
        _s3Service = s3Service;
        _notificationService = notificationService;
    }

    public async Task<VaultResponseDTO?> GetVaultByIdAsync(int vaultId, string userId)
    {
        var vault = await _dbContext.Vaults
            .Include(v => v.Owner)
            .Include(v => v.Policy)
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null)
            return null;

        // Owner always has access
        var isOwner = vault.OwnerId == userId;
        
        // Check if user is a member
        var privilege = await GetUserPrivilegeAsync(vaultId, userId);
        var isMember = privilege != null || isOwner;

        if (!isMember)
            return null;

        // Don't return deleted vaults unless user is owner
        if (vault.Status == VaultStatus.Deleted && !isOwner)
            return null;

        // Vault-level policy is superior - non-owners cannot access vault if policy blocks access
        // Owner always has access regardless of policy
        if (!isOwner && vault.Policy != null)
        {
            // Check general policy accessibility (this may update policy status)
            if (!await IsVaultAccessibleAsync(vault.Policy))
            {
                // Return null to block access - frontend will handle showing appropriate message
                return null;
            }
        }
        else if (vault.Policy != null)
        {
            // Even for owners, check and update policy status (e.g., TimeBased release)
            await IsVaultAccessibleAsync(vault.Policy);
        }

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
            UserPrivilege = privilege ?? (isOwner ? Privilege.Owner : null),
            Policy = vault.Policy != null ? MapPolicyToDTO(vault.Policy) : null
        };
    }

    public async Task<List<VaultResponseDTO>> GetUserVaultsAsync(string userId)
    {
        var vaults = await _dbContext.Vaults
            .Include(v => v.Members)
            .Include(v => v.Policy)
            .Where(v => v.Status == VaultStatus.Active && 
                       (v.OwnerId == userId || 
                        v.Members.Any(m => m.UserId == userId && m.Status == MemberStatus.Active)))
            .ToListAsync();

        var result = new List<VaultResponseDTO>();
        foreach (var vault in vaults)
        {
            var isOwner = vault.OwnerId == userId;
            var privilege = await GetUserPrivilegeAsync(vault.Id, userId);
            
            // Check and update policy status (e.g., TimeBased release, ExpiryBased expiration)
            if (vault.Policy != null)
            {
                await IsVaultAccessibleAsync(vault.Policy);
            }
            
            // Include all vaults (even if not accessible) so users can see when they'll get access
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
                UserPrivilege = privilege ?? (isOwner ? Privilege.Owner : null),
                Policy = vault.Policy != null ? MapPolicyToDTO(vault.Policy) : null
            });
        }

        return result;
    }

    public async Task<VaultResponseDTO> CreateVaultAsync(CreateVaultDTO dto, string userId)
    {
        // Validate policy configuration
        ValidatePolicyConfiguration(dto.PolicyType, dto.ReleaseDate, null, dto.ExpiresAt);

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

        // Create vault policy
        var policy = new VaultPolicy
        {
            VaultId = vault.Id,
            PolicyType = dto.PolicyType,
            // Set time to 9 AM UTC for date-only inputs
            ReleaseDate = dto.ReleaseDate.HasValue 
                ? dto.ReleaseDate.Value.Date.AddHours(9) 
                : null,
            ExpiresAt = dto.ExpiresAt.HasValue 
                ? dto.ExpiresAt.Value.Date.AddHours(9) 
                : null,
            CreatedAt = DateTime.UtcNow
        };

        // Determine initial release status based on policy type using the helper method
        SetPolicyReleaseStatus(policy);

        _dbContext.VaultPolicies.Add(policy);
        await _dbContext.SaveChangesAsync();

        // Load policy for response
        await _dbContext.Entry(vault).Reference(v => v.Policy).LoadAsync();

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = vault.Id,
            UserId = userId,
            Action = "CreateVault",
            Timestamp = DateTime.UtcNow,
            AdditionalContext = $"PolicyType: {dto.PolicyType}"
        };
        _dbContext.VaultLogs.Add(vaultLog);
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
            UserPrivilege = Privilege.Owner,
            Policy = vault.Policy != null ? MapPolicyToDTO(vault.Policy) : null
        };
    }

    public async Task<VaultResponseDTO?> UpdateVaultAsync(int vaultId, UpdateVaultDTO dto, string userId)
    {
        var vault = await _dbContext.Vaults
            .Include(v => v.Policy)
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null || vault.Status == VaultStatus.Deleted)
            return null;

        // Check permissions: Only Owner can edit vault and policy
        var privilege = await GetUserPrivilegeAsync(vaultId, userId);
        if (privilege != Privilege.Owner)
            return null;

        vault.Name = dto.Name;
        vault.Description = dto.Description;

        // Track if policy changed
        var oldPolicyType = vault.Policy?.PolicyType;
        var policyChanged = false;

        // Validate and update policy
        ValidatePolicyConfiguration(dto.PolicyType, dto.ReleaseDate, null, dto.ExpiresAt);

        if (vault.Policy == null)
        {
            // Create new policy
            vault.Policy = new VaultPolicy
            {
                VaultId = vault.Id,
                PolicyType = dto.PolicyType,
                // Set time to 9 AM UTC for date-only inputs
                ReleaseDate = dto.ReleaseDate.HasValue 
                    ? dto.ReleaseDate.Value.Date.AddHours(9) 
                    : null,
                ExpiresAt = dto.ExpiresAt.HasValue 
                    ? dto.ExpiresAt.Value.Date.AddHours(9) 
                    : null,
                CreatedAt = DateTime.UtcNow
            };
            SetPolicyReleaseStatus(vault.Policy);
            _dbContext.VaultPolicies.Add(vault.Policy);
            policyChanged = true;
        }
        else
        {
            // Update existing policy
            if (vault.Policy.PolicyType != dto.PolicyType)
            {
                policyChanged = true;
            }
            vault.Policy.PolicyType = dto.PolicyType;
            // Set time to 9 AM UTC for date-only inputs
            vault.Policy.ReleaseDate = dto.ReleaseDate.HasValue 
                ? dto.ReleaseDate.Value.Date.AddHours(9) 
                : null;
            vault.Policy.ExpiresAt = dto.ExpiresAt.HasValue 
                ? dto.ExpiresAt.Value.Date.AddHours(9) 
                : null;
            var oldReleaseStatus = vault.Policy.ReleaseStatus;
            SetPolicyReleaseStatus(vault.Policy);
            
            // Check if release status changed from non-released to released
            if (oldReleaseStatus != ReleaseStatus.Released && vault.Policy.ReleaseStatus == ReleaseStatus.Released)
            {
                // Send notification to all vault members
                try
                {
                    var members = await _dbContext.VaultMembers
                        .Include(m => m.User)
                        .Where(m => m.VaultId == vaultId && m.Status == MemberStatus.Active)
                        .ToListAsync();
                    
                    foreach (var member in members)
                    {
                        if (member.User != null && !string.IsNullOrEmpty(member.User.Email))
                        {
                            await _emailService.SendVaultReleasedNotificationAsync(
                                member.User.Email,
                                member.User.UserName ?? member.User.Email,
                                vault.Name
                            );
                            
                            // Save notification (trigger also creates one, but this ensures it's created)
                            await _notificationService.CreateNotificationAsync(
                                member.User.Id,
                                "Vault Released",
                                $"The vault \"{vault.Name}\" has been released and is now accessible.",
                                "VaultReleased",
                                vaultId: vaultId
                            );
                        }
                    }
                }
                catch
                {
                    // Log but don't fail the update
                }
            }
        }

        await _dbContext.SaveChangesAsync();

        // Reload policy
        await _dbContext.Entry(vault).Reference(v => v.Policy).LoadAsync();

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = vault.Id,
            UserId = userId,
            Action = "UpdateVault",
            Timestamp = DateTime.UtcNow
        };
        _dbContext.VaultLogs.Add(vaultLog);
        await _dbContext.SaveChangesAsync();

        // Send notification to owner if policy changed (owner is the one making the change, but send for record)
        if (policyChanged)
        {
            try
            {
                var owner = await _userManager.FindByIdAsync(vault.OwnerId);
                if (owner != null && !string.IsNullOrEmpty(owner.Email))
                {
                    await _emailService.SendVaultPolicyChangedNotificationAsync(
                        owner.Email,
                        owner.UserName ?? owner.Email,
                        vault.Name,
                        dto.PolicyType.ToString()
                    );
                    
                    // Save notification
                    await _notificationService.CreateNotificationAsync(
                        owner.Id,
                        "Vault Policy Changed",
                        $"The policy for vault \"{vault.Name}\" has been changed to {dto.PolicyType}.",
                        "VaultPolicyChanged",
                        vaultId: vaultId
                    );
                }
            }
            catch
            {
                // Log but don't fail the update
            }
        }

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
            UserPrivilege = Privilege.Owner,
            Policy = vault.Policy != null ? MapPolicyToDTO(vault.Policy) : null
        };
    }

    public async Task<bool> DeleteVaultAsync(int vaultId, string userId)
    {
        var vault = await _dbContext.Vaults
            .Include(v => v.Items)
                .ThenInclude(i => i.Document)
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null || vault.Status == VaultStatus.Deleted)
            return false;

        // Only owner can delete
        if (vault.OwnerId != userId)
            return false;

        // Delete all documents from S3 bucket before deleting items
        var itemsWithDocuments = vault.Items
            .Where(i => i.Document != null && i.ItemType == ItemType.Document)
            .ToList();

        foreach (var item in itemsWithDocuments)
        {
            if (item.Document != null && !string.IsNullOrEmpty(item.Document.ObjectKey))
            {
                try
                {
                    await _s3Service.DeleteFileAsync(item.Document.ObjectKey);
                    _logger.LogInformation("Deleted document {ObjectKey} from S3 for vault {VaultId}", 
                        item.Document.ObjectKey, vaultId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete document {ObjectKey} from S3 for vault {VaultId}. Continuing with vault deletion.", 
                        item.Document.ObjectKey, vaultId);
                    // Continue with deletion even if S3 deletion fails
                }
            }
        }

        // Log vault activity before deletion
        var vaultLog = new VaultLog
        {
            VaultId = vaultId,
            UserId = userId,
            Action = "DeleteVault",
            Timestamp = DateTime.UtcNow
        };
        _dbContext.VaultLogs.Add(vaultLog);
        await _dbContext.SaveChangesAsync();

        // Send notification to owner before deletion
        try
        {
            var owner = await _userManager.FindByIdAsync(vault.OwnerId);
            if (owner != null && !string.IsNullOrEmpty(owner.Email))
            {
                await _emailService.SendVaultDeletedNotificationAsync(
                    owner.Email,
                    owner.UserName ?? owner.Email,
                    vault.Name
                );
                
                // Save notification (before deletion)
                await _notificationService.CreateNotificationAsync(
                    owner.Id,
                    "Vault Deleted",
                    $"The vault \"{vault.Name}\" has been deleted. All items and data have been permanently removed.",
                    "VaultDeleted",
                    vaultId: vaultId
                );
            }
        }
        catch
        {
            // Log but don't fail the deletion
        }

        // Actually delete the vault (not archive) - cascade delete will handle items, members, invites, policy
        _dbContext.Vaults.Remove(vault);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Vault {VaultId} deleted by user {UserId}", vaultId, userId);
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

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = vaultId,
            UserId = userId,
            Action = "RestoreVault",
            Timestamp = DateTime.UtcNow
        };
        _dbContext.VaultLogs.Add(vaultLog);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<VaultInviteResponseDTO> CreateInviteAsync(int vaultId, CreateInviteDTO dto, string inviterId)
    {
        var vault = await _dbContext.Vaults
            .Include(v => v.Policy)
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null || vault.Status == VaultStatus.Deleted)
            throw new InvalidOperationException("Vault not found");

        // Only owners can invite - vault-level policy is superior
        var privilege = await GetUserPrivilegeAsync(vaultId, inviterId);
        var isOwner = vault.OwnerId == inviterId;
        if (!isOwner)
            throw new UnauthorizedAccessException("Only owners can invite members");

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
            Status = InviteStatus.Pending,
            // Set time to 9 AM UTC for date-only inputs, default to 7 days from now
            ExpiresAt = dto.InviteExpiresAt.HasValue 
                ? dto.InviteExpiresAt.Value.Date.AddHours(9) 
                : DateTime.UtcNow.Date.AddDays(7).AddHours(9),
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            Note = dto.Note
        };

        _dbContext.VaultInvites.Add(invite);
        await _dbContext.SaveChangesAsync();

        // Get inviter info before sending email
        var inviter = await _userManager.FindByIdAsync(inviterId);
        
        // Always send email immediately (invites are always immediate now)
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

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = vaultId,
            UserId = inviterId,
            Action = "CreateInvite",
            Timestamp = DateTime.UtcNow,
            TargetUserId = invite.InviteeId,
            AdditionalContext = $"InviteeEmail: {dto.InviteeEmail}, Privilege: {dto.Privilege}"
        };
        _dbContext.VaultLogs.Add(vaultLog);
        await _dbContext.SaveChangesAsync();

        // Send notification to vault owner if inviter is not the owner
        if (vault.OwnerId != inviterId)
        {
            try
            {
                var owner = await _userManager.FindByIdAsync(vault.OwnerId);
                var inviterName = inviter?.UserName ?? inviter?.Email ?? "Unknown";
                if (owner != null && !string.IsNullOrEmpty(owner.Email))
                {
                    await _emailService.SendInviteSentToOwnerNotificationAsync(
                        owner.Email,
                        owner.UserName ?? owner.Email,
                        vault.Name,
                        inviterName,
                        dto.InviteeEmail
                    );
                    
                    // Save notification
                    await _notificationService.CreateNotificationAsync(
                        owner.Id,
                        "New Invite Sent",
                        $"{inviterName} sent an invitation to {dto.InviteeEmail} for vault \"{vault.Name}\".",
                        "InviteSent",
                        vaultId: vaultId,
                        inviteId: invite.Id
                    );
                }
            }
            catch
            {
                // Log but don't fail the invite creation
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
            Status = invite.Status,
            ExpiresAt = invite.ExpiresAt,
            CreatedAt = invite.CreatedAt,
            AcceptedAt = invite.AcceptedAt,
            Note = invite.Note
        };
    }

    public async Task<List<VaultInviteResponseDTO>> GetVaultInvitesAsync(int vaultId, string userId)
    {
        var vault = await _dbContext.Vaults
            .Include(v => v.Policy)
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null)
            return new List<VaultInviteResponseDTO>();

        // Owner always has access
        var isOwner = vault.OwnerId == userId;

        // Check permissions: Only Owner can view invites (vault-level policy is superior)
        var privilege = await GetUserPrivilegeAsync(vaultId, userId);
        if (!isOwner)
            return new List<VaultInviteResponseDTO>();

        // Vault-level policy is superior - non-owners cannot access invites if policy blocks access
        if (!isOwner && vault.Policy != null)
        {
            if (!await IsVaultAccessibleAsync(vault.Policy))
                return new List<VaultInviteResponseDTO>();
        }
        else if (vault.Policy != null)
        {
            // Even for owners, check and update policy status
            await IsVaultAccessibleAsync(vault.Policy);
        }

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
            Status = i.Status,
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

        var vault = invite.Vault;
        var isOwner = vault.OwnerId == userId;
        
        // Vault-level policy is superior - only owners can manage invites
        if (!isOwner)
            return false;

        invite.Status = InviteStatus.Cancelled;
        await _dbContext.SaveChangesAsync();

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = invite.VaultId,
            UserId = userId,
            Action = "CancelInvite",
            Timestamp = DateTime.UtcNow,
            TargetUserId = invite.InviteeId
        };
        _dbContext.VaultLogs.Add(vaultLog);
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

        var vault = invite.Vault;
        var isOwner = vault.OwnerId == userId;
        
        // Vault-level policy is superior - only owners can manage invites
        if (!isOwner)
            return false;

        // Generate new token
        var token = GenerateInviteToken();
        invite.TokenHash = HashToken(token);
        invite.Status = InviteStatus.Pending;

        await _dbContext.SaveChangesAsync();

        // Send email
        await SendInviteEmailAsync(invite, token);

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = invite.VaultId,
            UserId = userId,
            Action = "ResendInvite",
            Timestamp = DateTime.UtcNow,
            TargetUserId = invite.InviteeId
        };
        _dbContext.VaultLogs.Add(vaultLog);
        await _dbContext.SaveChangesAsync();

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
            if (invite.Status != InviteStatus.Expired)
            {
                invite.Status = InviteStatus.Expired;
                await _dbContext.SaveChangesAsync();
                
                // Send notifications to invitee and inviter
                try
                {
                    var vault = await _dbContext.Vaults
                        .FirstOrDefaultAsync(v => v.Id == invite.VaultId);
                    
                    // Notify invitee
                    if (!string.IsNullOrEmpty(invite.InviteeEmail))
                    {
                        await _emailService.SendInviteExpiredNotificationAsync(
                            invite.InviteeEmail,
                            invite.InviteeEmail,
                            vault?.Name ?? "Unknown Vault",
                            false
                        );
                    }
                    
                    // Notify inviter
                    var inviter = await _userManager.FindByIdAsync(invite.InviterId);
                    if (inviter != null && !string.IsNullOrEmpty(inviter.Email))
                    {
                        await _emailService.SendInviteExpiredNotificationAsync(
                            inviter.Email,
                            inviter.UserName ?? inviter.Email,
                            vault?.Name ?? "Unknown Vault",
                            true
                        );
                    }
                }
                catch
                {
                    // Log but don't fail
                }
            }
            
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
            if (invite.Status != InviteStatus.Expired)
            {
                invite.Status = InviteStatus.Expired;
                await _dbContext.SaveChangesAsync();
                
                // Send notifications to invitee and inviter
                try
                {
                    var vault = await _dbContext.Vaults
                        .FirstOrDefaultAsync(v => v.Id == invite.VaultId);
                    
                    // Notify invitee
                    if (!string.IsNullOrEmpty(invite.InviteeEmail))
                    {
                        var inviteeUser = await _userManager.FindByEmailAsync(invite.InviteeEmail);
                        if (inviteeUser != null)
                        {
                            await _emailService.SendInviteExpiredNotificationAsync(
                                invite.InviteeEmail,
                                invite.InviteeEmail,
                                vault?.Name ?? "Unknown Vault",
                                false
                            );
                            
                            // Save notification
                            await _notificationService.CreateNotificationAsync(
                                inviteeUser.Id,
                                "Invitation Expired",
                                $"Your invitation to join vault \"{vault?.Name ?? "Unknown Vault"}\" has expired.",
                                "InviteExpired",
                                vaultId: invite.VaultId,
                                inviteId: invite.Id
                            );
                        }
                    }
                    
                    // Notify inviter
                    var inviter = await _userManager.FindByIdAsync(invite.InviterId);
                    if (inviter != null && !string.IsNullOrEmpty(inviter.Email))
                    {
                        await _emailService.SendInviteExpiredNotificationAsync(
                            inviter.Email,
                            inviter.UserName ?? inviter.Email,
                            vault?.Name ?? "Unknown Vault",
                            true
                        );
                        
                        // Save notification
                        await _notificationService.CreateNotificationAsync(
                            inviter.Id,
                            "Invitation Expired",
                            $"The invitation you sent for vault \"{vault?.Name ?? "Unknown Vault"}\" has expired.",
                            "InviteExpired",
                            vaultId: invite.VaultId,
                            inviteId: invite.Id
                        );
                    }
                }
                catch
                {
                    // Log but don't fail
                }
            }
            
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

        VaultMember memberRecord;

        if (anyExistingMember != null)
        {
            _logger.LogInformation("Found existing member record {MemberId} for user {UserId} in vault {VaultId}. Current status: {Status}", 
                anyExistingMember.Id, userId, invite.VaultId, anyExistingMember.Status);
            
            if (anyExistingMember.Status == MemberStatus.Active)
            {
                // Already active, just mark invite as accepted
                _logger.LogInformation("Member {UserId} is already active in vault {VaultId}", userId, invite.VaultId);
                memberRecord = anyExistingMember;
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
                memberRecord = anyExistingMember;
                
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
            memberRecord = member;
        }

        // Update invite
        invite.Status = InviteStatus.Accepted;
        invite.AcceptedAt = DateTime.UtcNow;
        invite.InviteeId = userId;

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Successfully saved member changes for {UserId} in vault {VaultId}", userId, invite.VaultId);

            // Log vault activity
            var vaultLog = new VaultLog
            {
                VaultId = invite.VaultId,
                UserId = userId,
                Action = "AcceptInvite",
                Timestamp = DateTime.UtcNow,
                TargetUserId = invite.InviterId,
                AdditionalContext = $"Privilege: {invite.Privilege}"
            };
            _dbContext.VaultLogs.Add(vaultLog);
            await _dbContext.SaveChangesAsync();

            // Send notification to vault owner
            try
            {
                var vault = await _dbContext.Vaults
                    .Include(v => v.Owner)
                    .FirstOrDefaultAsync(v => v.Id == invite.VaultId);
                var memberName = user?.UserName ?? user?.Email ?? "Unknown";
                
                if (vault != null && vault.Owner != null && !string.IsNullOrEmpty(vault.Owner.Email))
                {
                    await _emailService.SendInviteAcceptedToOwnerNotificationAsync(
                        vault.Owner.Email,
                        vault.Owner.UserName ?? vault.Owner.Email,
                        vault.Name,
                        memberName
                    );
                    
                    // Save notification
                    await _notificationService.CreateNotificationAsync(
                        vault.Owner.Id,
                        "New Member Joined",
                        $"{memberName} has accepted the invitation and joined vault \"{vault.Name}\".",
                        "InviteAccepted",
                        vaultId: invite.VaultId,
                        inviteId: invite.Id
                    );
                }
            }
            catch
            {
                // Log but don't fail the invite acceptance
            }

            // Step: Create VaultItemVisibility records for all existing items in the vault
            // New members should get View permission for all existing items
            // After SaveChangesAsync, memberRecord should have an ID (if it was newly created)
            // Refresh from database to ensure we have the ID
            var finalMemberRecord = await _dbContext.VaultMembers
                .FirstOrDefaultAsync(m => m.VaultId == invite.VaultId && m.UserId == userId);

            if (finalMemberRecord != null)
            {
                var existingItems = await _dbContext.VaultItems
                    .Where(i => i.VaultId == invite.VaultId && i.Status == ItemStatus.Active)
                    .ToListAsync();

                if (existingItems.Any())
                {
                    foreach (var item in existingItems)
                    {
                        // Check if visibility record already exists (shouldn't happen for new members, but safe check)
                        var existingVisibility = await _dbContext.VaultItemVisibilities
                            .FirstOrDefaultAsync(v => v.VaultItemId == item.Id && v.VaultMemberId == finalMemberRecord.Id);

                        if (existingVisibility == null)
                        {
                            var visibility = new VaultItemVisibility
                            {
                                VaultItemId = item.Id,
                                VaultMemberId = finalMemberRecord.Id,
                                Permission = ItemPermission.View // New members get View permission by default
                            };
                            _dbContext.VaultItemVisibilities.Add(visibility);
                        }
                    }
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Created {Count} visibility records for new member {UserId} in vault {VaultId}", 
                        existingItems.Count, userId, invite.VaultId);
                }
            }

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
        var vault = await _dbContext.Vaults
            .Include(v => v.Policy)
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null)
            return new List<VaultMemberResponseDTO>();

        // Owner always has access
        var isOwner = vault.OwnerId == userId;
        
        // Check if user has access to vault
        var privilege = await GetUserPrivilegeAsync(vaultId, userId);
        if (privilege == null && !isOwner)
            return new List<VaultMemberResponseDTO>();

        // Vault-level policy is superior - non-owners cannot access members if policy blocks access
        if (!isOwner && vault.Policy != null)
        {
            if (!await IsVaultAccessibleAsync(vault.Policy))
                return new List<VaultMemberResponseDTO>();
        }
        else if (vault.Policy != null)
        {
            // Even for owners, check and update policy status
            await IsVaultAccessibleAsync(vault.Policy);
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
                .ThenInclude(v => v.Policy)
            .FirstOrDefaultAsync(m => m.Id == memberId && m.VaultId == vaultId);

        if (member == null || member.Status != MemberStatus.Active)
            return false;

        var vault = member.Vault;
        var isOwner = vault.OwnerId == userId;
        var privilege = await GetUserPrivilegeAsync(vaultId, userId);

        // Vault-level policy is superior - only owners can manage members
        if (!isOwner)
            return false;

        // Owner can remove anyone (except themselves without transfer)
        // Owner cannot remove themselves
        if (member.UserId == userId)
            return false;

        member.Status = MemberStatus.Removed;
        member.RemovedById = userId;
        member.RemovedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = vaultId,
            UserId = userId,
            Action = "RemoveMember",
            Timestamp = DateTime.UtcNow,
            TargetUserId = member.UserId
        };
        _dbContext.VaultLogs.Add(vaultLog);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateMemberPrivilegeAsync(int vaultId, UpdateMemberPrivilegeDTO dto, string userId)
    {
        var member = await _dbContext.VaultMembers
            .Include(m => m.Vault)
                .ThenInclude(v => v.Policy)
            .FirstOrDefaultAsync(m => m.Id == dto.MemberId && m.VaultId == vaultId);

        if (member == null || member.Status != MemberStatus.Active)
            return false;

        var vault = member.Vault;
        var isOwner = vault.OwnerId == userId;

        // Vault-level policy is superior - only owners can manage members
        if (!isOwner)
            return false;

        // Cannot change owner privilege (must use transfer ownership)
        if (member.Privilege == Privilege.Owner && dto.Privilege != Privilege.Owner)
            return false;

        // If promoting to Owner, must use transfer ownership
        if (dto.Privilege == Privilege.Owner)
            return false;

        member.Privilege = dto.Privilege;
        await _dbContext.SaveChangesAsync();

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = vaultId,
            UserId = userId,
            Action = "UpdateMemberPrivilege",
            Timestamp = DateTime.UtcNow,
            TargetUserId = member.UserId,
            AdditionalContext = $"NewPrivilege: {dto.Privilege}"
        };
        _dbContext.VaultLogs.Add(vaultLog);
        await _dbContext.SaveChangesAsync();

        return true;
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

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = vaultId,
            UserId = userId,
            Action = "TransferOwnership",
            Timestamp = DateTime.UtcNow,
            TargetUserId = newOwner.UserId
        };
        _dbContext.VaultLogs.Add(vaultLog);
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

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = vaultId,
            UserId = userId,
            Action = "LeaveVault",
            Timestamp = DateTime.UtcNow
        };
        _dbContext.VaultLogs.Add(vaultLog);
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

    public async Task<bool> IsVaultAccessibleAsync(int vaultId, string userId)
    {
        var vault = await _dbContext.Vaults
            .Include(v => v.Policy)
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null)
            return false;

        // Owner always has access
        if (vault.OwnerId == userId)
            return true;

        // Check if user is a member
        var privilege = await GetUserPrivilegeAsync(vaultId, userId);
        if (privilege == null)
            return false;

        // Check vault policy
        if (vault.Policy == null)
            return true; // No policy means accessible

        return await IsVaultAccessibleAsync(vault.Policy);
    }

    // Helper methods
    private async Task<bool> IsVaultAccessibleAsync(VaultPolicy policy)
    {
        if (policy.ReleaseStatus == ReleaseStatus.Expired || policy.ReleaseStatus == ReleaseStatus.Revoked)
            return false;

        if (policy.ReleaseStatus == ReleaseStatus.Released)
        {
            // Check if expired (for ExpiryBased)
            if (policy.PolicyType == PolicyType.ExpiryBased && policy.ExpiresAt.HasValue)
            {
                if (DateTime.UtcNow > policy.ExpiresAt.Value)
                {
                    policy.ReleaseStatus = ReleaseStatus.Expired;
                    await _dbContext.SaveChangesAsync();
                    return false;
                }
            }

            return true;
        }

        // Check if TimeBased policy should be released now
        if (policy.PolicyType == PolicyType.TimeBased && policy.ReleaseDate.HasValue)
        {
            if (DateTime.UtcNow >= policy.ReleaseDate.Value)
            {
                var wasReleased = policy.ReleaseStatus == ReleaseStatus.Released;
                policy.ReleaseStatus = ReleaseStatus.Released;
                policy.ReleasedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                
                // Send notification to all vault members if status just changed to released
                if (!wasReleased)
                {
                    try
                    {
                        var vault = await _dbContext.Vaults
                            .FirstOrDefaultAsync(v => v.Id == policy.VaultId);
                        
                        if (vault != null)
                        {
                            var members = await _dbContext.VaultMembers
                                .Include(m => m.User)
                                .Where(m => m.VaultId == policy.VaultId && m.Status == MemberStatus.Active)
                                .ToListAsync();
                            
                            foreach (var member in members)
                            {
                                if (member.User != null && !string.IsNullOrEmpty(member.User.Email))
                                {
                                    await _emailService.SendVaultReleasedNotificationAsync(
                                        member.User.Email,
                                        member.User.UserName ?? member.User.Email,
                                        vault.Name
                                    );
                                    
                                    // Save notification (trigger also creates one, but this ensures it's created)
                                    await _notificationService.CreateNotificationAsync(
                                        member.User.Id,
                                        "Vault Released",
                                        $"The vault \"{vault.Name}\" has been released and is now accessible.",
                                        "VaultReleased",
                                        vaultId: policy.VaultId
                                    );
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Log but don't fail
                    }
                }
                
                return true;
            }
            return false;
        }

        // For Pending status: ManualRelease requires owner action, so non-owners cannot access
        // TimeBased is handled above
        if (policy.ReleaseStatus == ReleaseStatus.Pending)
        {
            // TimeBased is already handled above
            // For ManualRelease, return false (not accessible until owner acts)
            if (policy.PolicyType == PolicyType.ManualRelease)
            {
                return false;
            }
        }

        return false;
    }

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
            
            // Update invite status to Sent (for tracking purposes)
            invite.Status = InviteStatus.Sent;
            
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

    private void ValidatePolicyConfiguration(PolicyType policyType, DateTime? releaseDate, int? pulseIntervalDays, DateTime? expiresAt)
    {
        switch (policyType)
        {
            case PolicyType.TimeBased:
                if (!releaseDate.HasValue)
                    throw new ArgumentException("ReleaseDate is required for TimeBased policy");
                // Compare dates only (ignore time) - must be future (not today)
                if (releaseDate.Value.Date <= DateTime.UtcNow.Date)
                    throw new ArgumentException("ReleaseDate must be in the future for TimeBased policy");
                break;
            
            case PolicyType.ExpiryBased:
                if (!expiresAt.HasValue)
                    throw new ArgumentException("ExpiresAt is required for ExpiryBased policy");
                // Compare dates only (ignore time) - must be future (not today)
                if (expiresAt.Value.Date <= DateTime.UtcNow.Date)
                    throw new ArgumentException("ExpiresAt must be in the future for ExpiryBased policy");
                break;
            
            case PolicyType.Immediate:
            case PolicyType.ManualRelease:
                // No additional validation needed
                break;
        }
    }

    public async Task<bool> ReleaseVaultManuallyAsync(int vaultId, string userId)
    {
        var vault = await _dbContext.Vaults
            .Include(v => v.Policy)
            .FirstOrDefaultAsync(v => v.Id == vaultId);

        if (vault == null || vault.Policy == null)
            return false;

        // Check permissions: Only Owner can manually release
        var privilege = await GetUserPrivilegeAsync(vaultId, userId);
        if (privilege != Privilege.Owner)
            return false;

        if (vault.Policy.PolicyType != PolicyType.ManualRelease)
            return false;

        if (vault.Policy.ReleaseStatus != ReleaseStatus.Pending)
            return false;

        vault.Policy.ReleaseStatus = ReleaseStatus.Released;
        vault.Policy.ReleasedAt = DateTime.UtcNow;
        vault.Policy.ReleasedById = userId;

        await _dbContext.SaveChangesAsync();

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = vaultId,
            UserId = userId,
            Action = "ReleaseVaultManually",
            Timestamp = DateTime.UtcNow
        };
        _dbContext.VaultLogs.Add(vaultLog);
        await _dbContext.SaveChangesAsync();

        // Send notification to all vault members
        try
        {
            var members = await _dbContext.VaultMembers
                .Include(m => m.User)
                .Where(m => m.VaultId == vaultId && m.Status == MemberStatus.Active)
                .ToListAsync();
            
            foreach (var member in members)
            {
                if (member.User != null && !string.IsNullOrEmpty(member.User.Email))
                {
                    await _emailService.SendVaultReleasedNotificationAsync(
                        member.User.Email,
                        member.User.UserName ?? member.User.Email,
                        vault.Name
                    );
                    
                    // Save notification (trigger also creates one, but this ensures it's created)
                    await _notificationService.CreateNotificationAsync(
                        member.User.Id,
                        "Vault Released",
                        $"The vault \"{vault.Name}\" has been released and is now accessible.",
                        "VaultReleased",
                        vaultId: vaultId
                    );
                }
            }
        }
        catch
        {
            // Log but don't fail the release
        }

        return true;
    }

    public async Task<List<VaultLogResponseDTO>> GetVaultLogsAsync(int vaultId, string userId)
    {
        // Check if user has access to the vault
        var hasAccess = await CanViewVaultAsync(vaultId, userId);
        if (!hasAccess)
            throw new UnauthorizedAccessException("Access denied to vault");

        var logs = await _dbContext.VaultLogs
            .Include(l => l.User)
            .Include(l => l.TargetUser)
            .Where(l => l.VaultId == vaultId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();

        return logs.Select(log => new VaultLogResponseDTO
        {
            Id = log.Id,
            VaultId = log.VaultId,
            UserId = log.UserId,
            UserEmail = log.User?.Email,
            UserName = log.User?.UserName,
            TargetUserId = log.TargetUserId,
            TargetUserEmail = log.TargetUser?.Email,
            TargetUserName = log.TargetUser?.UserName,
            ItemId = log.ItemId,
            Action = log.Action,
            Timestamp = log.Timestamp,
            AdditionalContext = log.AdditionalContext
        }).ToList();
    }

    // Helper methods for policy

    private VaultPolicyResponseDTO MapPolicyToDTO(VaultPolicy policy)
    {
        return new VaultPolicyResponseDTO
        {
            PolicyType = policy.PolicyType,
            ReleaseStatus = policy.ReleaseStatus,
            ReleaseDate = policy.ReleaseDate,
            ExpiresAt = policy.ExpiresAt,
            ReleasedAt = policy.ReleasedAt
        };
    }

    private void SetPolicyReleaseStatus(VaultPolicy policy)
    {
        switch (policy.PolicyType)
        {
            case PolicyType.Immediate:
                policy.ReleaseStatus = ReleaseStatus.Released;
                policy.ReleasedAt = DateTime.UtcNow;
                break;
            
            case PolicyType.TimeBased:
                policy.ReleaseStatus = ReleaseStatus.Pending;
                // Will be released when ReleaseDate is reached
                break;
            
            case PolicyType.ExpiryBased:
                policy.ReleaseStatus = ReleaseStatus.Released;
                policy.ReleasedAt = DateTime.UtcNow;
                // Will expire at ExpiresAt
                break;
            
            case PolicyType.ManualRelease:
                policy.ReleaseStatus = ReleaseStatus.Pending;
                // Requires manual release
                break;
        }
    }
}

