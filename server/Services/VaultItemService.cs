using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using server.Dtos.VaultItem;
using server.Interfaces;
using server.Models;

namespace server.Services;

public class VaultItemService : IVaultItemService
{
    private readonly ApplicationDBContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly IVaultService _vaultService;
    private readonly IEncryptionService _encryptionService;
    private readonly IS3Service _s3Service;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public VaultItemService(
        ApplicationDBContext dbContext,
        UserManager<User> userManager,
        IVaultService vaultService,
        IEncryptionService encryptionService,
        IS3Service s3Service,
        IConfiguration configuration,
        IEmailService emailService,
        INotificationService notificationService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _vaultService = vaultService;
        _encryptionService = encryptionService;
        _s3Service = s3Service;
        _configuration = configuration;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    public async Task<VaultItemResponseDTO?> GetItemByIdAsync(int itemId, string userId)
    {
        var item = await _dbContext.VaultItems
            .Include(i => i.CreatedByUser)
            .Include(i => i.Visibilities)
                .ThenInclude(v => v.VaultMember)
                    .ThenInclude(m => m.User)
            .Include(i => i.Document)
            .Include(i => i.Password)
            .Include(i => i.Note)
            .Include(i => i.Link)
            .Include(i => i.CryptoWallet)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null || item.Status == ItemStatus.Deleted)
            return null;

        // Check if user has access
        var permission = await GetUserPermissionAsync(itemId, userId);
        if (permission == null)
            return null;

        return await MapToResponseDTO(item, userId);
    }

    public async Task<List<VaultItemResponseDTO>> GetVaultItemsAsync(int vaultId, string userId)
    {
        // Check if user has access to vault
        var vaultPrivilege = await _vaultService.GetUserPrivilegeAsync(vaultId, userId);
        if (vaultPrivilege == null)
            return new List<VaultItemResponseDTO>();

        // Check vault policy - owner always has access, others need policy to allow access
        if (!await _vaultService.IsVaultAccessibleAsync(vaultId, userId))
        {
            // Return empty list - user can see vault but not items
            return new List<VaultItemResponseDTO>();
        }

        var items = await _dbContext.VaultItems
            .Include(i => i.CreatedByUser)
            .Include(i => i.Visibilities)
                .ThenInclude(v => v.VaultMember)
                    .ThenInclude(m => m.User)
            .Include(i => i.Document)
            .Include(i => i.Password)
            .Include(i => i.Note)
            .Include(i => i.Link)
            .Include(i => i.CryptoWallet)
            .Where(i => i.VaultId == vaultId && i.Status == ItemStatus.Active)
            .ToListAsync();

        var result = new List<VaultItemResponseDTO>();
        foreach (var item in items)
        {
            var permission = await GetUserPermissionForItemAsync(item, userId);
            if (permission != null)
            {
                result.Add(await MapToResponseDTO(item, userId));
            }
        }

        return result;
    }

    public async Task<VaultItemResponseDTO> CreateItemAsync(CreateVaultItemDTO dto, string userId)
    {
        // Check if user has access to vault
        var vaultPrivilege = await _vaultService.GetUserPrivilegeAsync(dto.VaultId, userId);
        if (vaultPrivilege == null)
            throw new UnauthorizedAccessException("You don't have access to this vault");

        // Check vault-level policy - vault-level policy is superior to item-level permissions
        // Owner always has access, but admins and members must respect vault policy
        if (!await _vaultService.IsVaultAccessibleAsync(dto.VaultId, userId))
            throw new UnauthorizedAccessException("Vault is not accessible due to its release policy. You cannot add items until the vault is released.");

        // Get vault encryption key (in production, this should come from user's master key)
        var encryptionKey = await GetVaultEncryptionKeyAsync(dto.VaultId, userId);

        var item = new VaultItem
        {
            VaultId = dto.VaultId,
            CreatedByUserId = userId,
            ItemType = dto.ItemType,
            Title = dto.Title,
            Description = dto.Description,
            Status = ItemStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.VaultItems.Add(item);
        await _dbContext.SaveChangesAsync();

        // Create item-specific data
        switch (dto.ItemType)
        {
            case ItemType.Document:
                if (dto.DocumentFile == null)
                    throw new InvalidOperationException("Document file is required for Document item type");
                await CreateDocumentAsync(item, dto.DocumentFile, encryptionKey);
                break;
            case ItemType.Password:
                await CreatePasswordAsync(item, dto, encryptionKey);
                break;
            case ItemType.Note:
                await CreateNoteAsync(item, dto, encryptionKey);
                break;
            case ItemType.Link:
                await CreateLinkAsync(item, dto, encryptionKey);
                break;
            case ItemType.CryptoWallet:
                await CreateCryptoWalletAsync(item, dto, encryptionKey);
                break;
        }

        // Set visibility permissions - ALWAYS create for all members
        if (dto.Visibilities != null && dto.Visibilities.Any())
        {
            // Ensure all members have visibility records
            await SetItemVisibilitiesAsync(item.Id, dto.Visibilities, dto.VaultId, userId);
        }
        else
        {
            // Default: make visible to all vault members (creator gets Edit, others get View)
            await SetDefaultVisibilitiesAsync(item.Id, dto.VaultId, userId);
        }

        await _dbContext.SaveChangesAsync();

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = dto.VaultId,
            UserId = userId,
            Action = "CreateItem",
            Timestamp = DateTime.UtcNow,
            ItemId = item.Id,
            AdditionalContext = $"Title: {item.Title}, ItemType: {dto.ItemType}"
        };
        _dbContext.VaultLogs.Add(vaultLog);
        await _dbContext.SaveChangesAsync();

        return await GetItemByIdAsync(item.Id, userId) ?? throw new InvalidOperationException("Failed to retrieve created item");
    }

    public async Task<VaultItemResponseDTO?> UpdateItemAsync(int itemId, UpdateVaultItemDTO dto, string userId)
    {
        var item = await _dbContext.VaultItems
            .Include(i => i.Document)
            .Include(i => i.Password)
            .Include(i => i.Note)
            .Include(i => i.Link)
            .Include(i => i.CryptoWallet)
            .Include(i => i.Visibilities)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null || item.Status == ItemStatus.Deleted)
            return null;

        // Check vault-level policy - vault-level policy is superior to item-level permissions
        // Owner always has access, but admins and members must respect vault policy
        if (!await _vaultService.IsVaultAccessibleAsync(item.VaultId, userId))
            throw new UnauthorizedAccessException("Vault is not accessible due to its release policy. You cannot edit items until the vault is released.");

        // Check if user can edit - must have Edit permission in VaultItemVisibility
        var permission = await GetUserPermissionAsync(itemId, userId);
        if (permission != ItemPermission.Edit)
            throw new UnauthorizedAccessException("You don't have permission to edit this item");

        var encryptionKey = await GetVaultEncryptionKeyAsync(item.VaultId, userId);

        // Track changes for logging
        var changedFields = new List<string>();

        // Track common field changes
        if (!string.IsNullOrEmpty(dto.Title) && dto.Title != item.Title)
        {
            changedFields.Add("Title");
            item.Title = dto.Title;
        }
        if (dto.Description != null && dto.Description != item.Description)
        {
            changedFields.Add("Description");
            item.Description = dto.Description;
        }
        item.UpdatedAt = DateTime.UtcNow;

        // Track item-specific field changes
        switch (item.ItemType)
        {
            case ItemType.Document:
                var docChanges = await UpdateDocumentAsync(item, dto, encryptionKey);
                changedFields.AddRange(docChanges);
                break;
            case ItemType.Password:
                var passwordChanges = await UpdatePasswordAsync(item, dto, encryptionKey);
                changedFields.AddRange(passwordChanges);
                break;
            case ItemType.Note:
                var noteChanges = await UpdateNoteAsync(item, dto, encryptionKey);
                changedFields.AddRange(noteChanges);
                break;
            case ItemType.Link:
                var linkChanges = await UpdateLinkAsync(item, dto, encryptionKey);
                changedFields.AddRange(linkChanges);
                break;
            case ItemType.CryptoWallet:
                var walletChanges = await UpdateCryptoWalletAsync(item, dto, encryptionKey);
                changedFields.AddRange(walletChanges);
                break;
        }

        // Track visibility changes - only log if permissions actually changed
        if (dto.Visibilities != null && dto.Visibilities.Any())
        {
            // Get vault to identify owner
            var vault = await _dbContext.Vaults
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == item.VaultId);
            
            // Get all vault members to identify owners
            var vaultMembers = await _dbContext.VaultMembers
                .Where(m => m.VaultId == item.VaultId && m.Status == MemberStatus.Active)
                .ToListAsync();
            
            var ownerMemberIds = vaultMembers
                .Where(m => m.UserId == vault?.OwnerId)
                .Select(m => m.Id)
                .ToHashSet();
            
            // Get current visibilities as a dictionary for comparison (exclude owners)
            var currentVisibilities = item.Visibilities
                .Where(v => !ownerMemberIds.Contains(v.VaultMemberId))
                .ToDictionary(v => v.VaultMemberId, v => v.Permission);
            
            // Get new visibilities as a dictionary (frontend doesn't send owners)
            var newVisibilities = dto.Visibilities
                .GroupBy(v => v.VaultMemberId)
                .ToDictionary(g => g.Key, g => g.Last().Permission);
            
            // Check if permissions actually changed
            bool permissionsChanged = false;
            
            // Check if any existing permission changed or was removed
            foreach (var current in currentVisibilities)
            {
                if (!newVisibilities.ContainsKey(current.Key))
                {
                    permissionsChanged = true;
                    break;
                }
                if (newVisibilities[current.Key] != current.Value)
                {
                    permissionsChanged = true;
                    break;
                }
            }
            
            // Check if any new permissions were added
            if (!permissionsChanged)
            {
                foreach (var newVis in newVisibilities.Keys)
                {
                    if (!currentVisibilities.ContainsKey(newVis))
                    {
                        permissionsChanged = true;
                        break;
                    }
                }
            }
            
            if (permissionsChanged)
            {
                changedFields.Add("Permissions");
            }
            
            await UpdateItemVisibilitiesAsync(itemId, dto.Visibilities, item.VaultId, userId);
        }

        await _dbContext.SaveChangesAsync();

        // Build log context with changed fields
        var logContext = $"Title: {item.Title}";
        if (changedFields.Any())
        {
            logContext += $", ChangedFields: {string.Join(", ", changedFields)}";
        }

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = item.VaultId,
            UserId = userId,
            Action = "UpdateItem",
            Timestamp = DateTime.UtcNow,
            ItemId = itemId,
            AdditionalContext = logContext
        };
        _dbContext.VaultLogs.Add(vaultLog);
        await _dbContext.SaveChangesAsync();

        // Send notification to item owner if edited by someone else
        if (item.CreatedByUserId != userId)
        {
            try
            {
                var vault = await _dbContext.Vaults
                    .Include(v => v.Owner)
                    .FirstOrDefaultAsync(v => v.Id == item.VaultId);
                var editor = await _userManager.FindByIdAsync(userId);
                var editorName = editor?.UserName ?? editor?.Email ?? "Unknown";
                
                if (vault != null && item.CreatedByUserId != null)
                {
                    var itemOwner = await _userManager.FindByIdAsync(item.CreatedByUserId);
                    if (itemOwner != null && !string.IsNullOrEmpty(itemOwner.Email))
                    {
                        await _emailService.SendVaultItemChangedNotificationAsync(
                            itemOwner.Email,
                            itemOwner.UserName ?? itemOwner.Email,
                            vault.Name,
                            item.Title,
                            "edited",
                            editorName
                        );
                        
                        // Save notification
                        await _notificationService.CreateNotificationAsync(
                            itemOwner.Id,
                            "Item Edited",
                            $"{editorName} edited the item \"{item.Title}\" in vault \"{vault.Name}\"",
                            "ItemEdited",
                            vaultId: vault.Id,
                            itemId: item.Id
                        );
                    }
                }
            }
            catch
            {
                // Log but don't fail the update
            }
        }

        return await GetItemByIdAsync(itemId, userId);
    }

    public async Task<bool> DeleteItemAsync(int itemId, string userId)
    {
        var item = await _dbContext.VaultItems
            .Include(i => i.Document)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null || item.Status == ItemStatus.Deleted)
            return false;

        // Check vault-level policy - vault-level policy is superior to item-level permissions
        // Owner always has access, but admins and members must respect vault policy
        if (!await _vaultService.IsVaultAccessibleAsync(item.VaultId, userId))
            return false;

        // Check if user can edit
        var permission = await GetUserPermissionAsync(itemId, userId);
        if (permission != ItemPermission.Edit)
            return false;

        item.Status = ItemStatus.Deleted;
        item.DeletedAt = DateTime.UtcNow;
        item.DeletedBy = userId;

        // Optionally delete document from S3
        if (item.Document != null && item.ItemType == ItemType.Document)
        {
            await _s3Service.DeleteFileAsync(item.Document.ObjectKey);
        }

        await _dbContext.SaveChangesAsync();

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = item.VaultId,
            UserId = userId,
            Action = "DeleteItem",
            Timestamp = DateTime.UtcNow,
            ItemId = itemId,
            AdditionalContext = $"Title: {item.Title}"
        };
        _dbContext.VaultLogs.Add(vaultLog);
        await _dbContext.SaveChangesAsync();

        // Send notification to item owner if deleted by someone else
        if (item.CreatedByUserId != userId)
        {
            try
            {
                var vault = await _dbContext.Vaults
                    .Include(v => v.Owner)
                    .FirstOrDefaultAsync(v => v.Id == item.VaultId);
                var deleter = await _userManager.FindByIdAsync(userId);
                var deleterName = deleter?.UserName ?? deleter?.Email ?? "Unknown";
                
                if (vault != null && item.CreatedByUserId != null)
                {
                    var itemOwner = await _userManager.FindByIdAsync(item.CreatedByUserId);
                    if (itemOwner != null && !string.IsNullOrEmpty(itemOwner.Email))
                    {
                        await _emailService.SendVaultItemChangedNotificationAsync(
                            itemOwner.Email,
                            itemOwner.UserName ?? itemOwner.Email,
                            vault.Name,
                            item.Title,
                            "deleted",
                            deleterName
                        );
                        
                        // Save notification
                        await _notificationService.CreateNotificationAsync(
                            itemOwner.Id,
                            "Item Deleted",
                            $"{deleterName} deleted the item \"{item.Title}\" in vault \"{vault.Name}\"",
                            "ItemDeleted",
                            vaultId: vault.Id,
                            itemId: itemId
                        );
                    }
                }
            }
            catch
            {
                // Log but don't fail the delete
            }
        }

        return true;
    }

    public async Task<bool> RestoreItemAsync(int itemId, string userId)
    {
        var item = await _dbContext.VaultItems.FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null || item.Status != ItemStatus.Deleted)
            return false;

        // Check vault-level policy - vault-level policy is superior to item-level permissions
        // Owner always has access, but admins and members must respect vault policy
        if (!await _vaultService.IsVaultAccessibleAsync(item.VaultId, userId))
            return false;

        // Check if user can edit vault
        var canEdit = await _vaultService.CanEditVaultAsync(item.VaultId, userId);
        if (!canEdit)
            return false;

        // Check if within 30 days recovery window
        if (item.DeletedAt.HasValue && (DateTime.UtcNow - item.DeletedAt.Value).TotalDays > 30)
            return false;

        item.Status = ItemStatus.Active;
        item.DeletedAt = null;
        item.DeletedBy = null;
        item.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        // Log vault activity
        var vaultLog = new VaultLog
        {
            VaultId = item.VaultId,
            UserId = userId,
            Action = "RestoreItem",
            Timestamp = DateTime.UtcNow,
            ItemId = itemId,
            AdditionalContext = $"Title: {item.Title}"
        };
        _dbContext.VaultLogs.Add(vaultLog);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<ItemPermission?> GetUserPermissionAsync(int itemId, string userId)
    {
        var item = await _dbContext.VaultItems
            .Include(i => i.Visibilities)
                .ThenInclude(v => v.VaultMember)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null)
            return null;

        return await GetUserPermissionForItemAsync(item, userId);
    }

    public async Task<bool> CanViewItemAsync(int itemId, string userId)
    {
        var permission = await GetUserPermissionAsync(itemId, userId);
        return permission != null;
    }

    public async Task<bool> CanEditItemAsync(int itemId, string userId)
    {
        var permission = await GetUserPermissionAsync(itemId, userId);
        return permission == ItemPermission.Edit;
    }

    // Helper methods
    private async Task<string> GetVaultEncryptionKeyAsync(int vaultId, string userId)
    {
        // Get vault owner ID to ensure all users in the vault use the same encryption key
        // This allows items created by any member to be decrypted by all authorized users
        var vault = await _dbContext.Vaults
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vaultId);
        
        if (vault == null)
            throw new InvalidOperationException($"Vault {vaultId} not found");

        // Use vault owner's ID to derive the encryption key
        // This ensures all items in the vault are encrypted/decrypted with the same key
        var keyMaterial = $"{vaultId}_{vault.OwnerId}_{_configuration["Jwt:SigningKey"]}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keyMaterial));
        return Convert.ToBase64String(hash);
    }

    private async Task CreateDocumentAsync(VaultItem item, IFormFile file, string encryptionKey)
    {
        var documentId = Guid.NewGuid().ToString();
        var objectKey = $"vaults/{item.VaultId}/documents/{documentId}/{file.FileName}";

        await _s3Service.UploadFileAsync(file, objectKey);

        var document = new VaultDocument
        {
            VaultItemId = item.Id,
            ObjectKey = objectKey,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        _dbContext.VaultDocuments.Add(document);
    }

    private async Task CreatePasswordAsync(VaultItem item, CreateVaultItemDTO dto, string encryptionKey)
    {
        var password = new VaultPassword
        {
            VaultItemId = item.Id,
            Username = dto.Username,
            EncryptedPassword = !string.IsNullOrEmpty(dto.Password) ? _encryptionService.Encrypt(dto.Password, encryptionKey) : string.Empty,
            WebsiteUrl = dto.WebsiteUrl,
            Notes = !string.IsNullOrEmpty(dto.PasswordNotes) ? _encryptionService.Encrypt(dto.PasswordNotes, encryptionKey) : null
        };

        _dbContext.VaultPasswords.Add(password);
        await Task.CompletedTask;
    }

    private async Task CreateNoteAsync(VaultItem item, CreateVaultItemDTO dto, string encryptionKey)
    {
        var note = new VaultNote
        {
            VaultItemId = item.Id,
            EncryptedContent = !string.IsNullOrEmpty(dto.NoteContent) ? _encryptionService.Encrypt(dto.NoteContent, encryptionKey) : string.Empty,
            ContentFormat = dto.ContentFormat ?? ContentFormat.PlainText
        };

        _dbContext.VaultNotes.Add(note);
        await Task.CompletedTask;
    }

    private async Task CreateLinkAsync(VaultItem item, CreateVaultItemDTO dto, string encryptionKey)
    {
        var link = new VaultLink
        {
            VaultItemId = item.Id,
            Url = dto.Url ?? string.Empty,
            Notes = !string.IsNullOrEmpty(dto.LinkNotes) ? _encryptionService.Encrypt(dto.LinkNotes, encryptionKey) : null
        };

        _dbContext.VaultLinks.Add(link);
        await Task.CompletedTask;
    }

    private async Task CreateCryptoWalletAsync(VaultItem item, CreateVaultItemDTO dto, string encryptionKey)
    {
        var wallet = new VaultCryptoWallet
        {
            VaultItemId = item.Id,
            WalletType = dto.WalletType ?? WalletType.SeedPhrase,
            PlatformName = dto.PlatformName,
            Blockchain = dto.Blockchain,
            PublicAddress = dto.PublicAddress,
            EncryptedSecret = !string.IsNullOrEmpty(dto.Secret) ? _encryptionService.Encrypt(dto.Secret, encryptionKey) : string.Empty,
            Notes = !string.IsNullOrEmpty(dto.CryptoNotes) ? _encryptionService.Encrypt(dto.CryptoNotes, encryptionKey) : null
        };

        _dbContext.VaultCryptoWallets.Add(wallet);
        await Task.CompletedTask;
    }

    private async Task SetItemVisibilitiesAsync(int itemId, List<ItemVisibilityDTO> visibilities, int vaultId, string userId)
    {
        // Remove existing visibilities (shouldn't be any for new items, but safe to do)
        var existing = await _dbContext.VaultItemVisibilities
            .Where(v => v.VaultItemId == itemId)
            .ToListAsync();
        if (existing.Any())
        {
            _dbContext.VaultItemVisibilities.RemoveRange(existing);
            await _dbContext.SaveChangesAsync();
        }

        // Get vault to find owner
        var vault = await _dbContext.Vaults
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vaultId);
        
        if (vault == null)
            throw new InvalidOperationException($"Vault {vaultId} not found");

        // Get all active members to ensure everyone has a visibility record
        var allMembers = await _dbContext.VaultMembers
            .Where(m => m.VaultId == vaultId && m.Status == MemberStatus.Active)
            .ToListAsync();

        // Create a dictionary of provided visibilities for quick lookup
        // Handle duplicates by taking the last one (in case frontend sends duplicates)
        var visibilityDict = visibilities != null && visibilities.Any()
            ? visibilities
                .GroupBy(v => v.VaultMemberId)
                .ToDictionary(g => g.Key, g => g.Last().Permission)
            : new Dictionary<int, ItemPermission>();

        // Add visibility records for all members
        foreach (var member in allMembers)
        {
            // Owner always gets Edit permission, regardless of what's provided
            ItemPermission permission;
            if (member.UserId == vault.OwnerId)
            {
                permission = ItemPermission.Edit;
            }
            else
            {
                // Use provided permission if available, otherwise default (creator gets Edit, others get View)
                permission = visibilityDict.ContainsKey(member.Id) 
                    ? visibilityDict[member.Id]
                    : (member.UserId == userId ? ItemPermission.Edit : ItemPermission.View);
            }

            var itemVisibility = new VaultItemVisibility
            {
                VaultItemId = itemId,
                VaultMemberId = member.Id,
                Permission = permission
            };
            _dbContext.VaultItemVisibilities.Add(itemVisibility);
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task SetDefaultVisibilitiesAsync(int itemId, int vaultId, string userId)
    {
        // Get vault to find owner
        var vault = await _dbContext.Vaults
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vaultId);
        
        if (vault == null)
            throw new InvalidOperationException($"Vault {vaultId} not found");

        // Get all active members of the vault
        var members = await _dbContext.VaultMembers
            .Where(m => m.VaultId == vaultId && m.Status == MemberStatus.Active)
            .ToListAsync();

        foreach (var member in members)
        {
            // Owner always gets Edit, creator gets Edit, others get View
            var permission = (member.UserId == vault.OwnerId || member.UserId == userId) 
                ? ItemPermission.Edit 
                : ItemPermission.View;
            var visibility = new VaultItemVisibility
            {
                VaultItemId = itemId,
                VaultMemberId = member.Id,
                Permission = permission
            };
            _dbContext.VaultItemVisibilities.Add(visibility);
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task<List<string>> UpdateDocumentAsync(VaultItem item, UpdateVaultItemDTO dto, string encryptionKey)
    {
        var changes = new List<string>();

        if (dto.DeleteDocument == true && item.Document != null)
        {
            changes.Add("Document (deleted)");
            await _s3Service.DeleteFileAsync(item.Document.ObjectKey);
            _dbContext.VaultDocuments.Remove(item.Document);
        }

        if (dto.DocumentFile != null)
        {
            if (item.Document != null)
            {
                changes.Add("Document (replaced)");
                await _s3Service.DeleteFileAsync(item.Document.ObjectKey);
                _dbContext.VaultDocuments.Remove(item.Document);
            }
            else
            {
                changes.Add("Document (added)");
            }

            await CreateDocumentAsync(item, dto.DocumentFile, encryptionKey);
        }

        return changes;
    }

    private async Task<List<string>> UpdatePasswordAsync(VaultItem item, UpdateVaultItemDTO dto, string encryptionKey)
    {
        var changes = new List<string>();

        if (item.Password == null)
        {
            item.Password = new VaultPassword { VaultItemId = item.Id };
            _dbContext.VaultPasswords.Add(item.Password);
        }

        if (dto.Username != null && dto.Username != item.Password.Username)
        {
            changes.Add("Username");
            item.Password.Username = dto.Username;
        }
        if (dto.Password != null)
        {
            changes.Add("Password");
            item.Password.EncryptedPassword = _encryptionService.Encrypt(dto.Password, encryptionKey);
        }
        if (dto.WebsiteUrl != null && dto.WebsiteUrl != item.Password.WebsiteUrl)
        {
            changes.Add("Website URL");
            item.Password.WebsiteUrl = dto.WebsiteUrl;
        }
        if (dto.PasswordNotes != null)
        {
            var newNotes = !string.IsNullOrEmpty(dto.PasswordNotes) ? _encryptionService.Encrypt(dto.PasswordNotes, encryptionKey) : null;
            var oldNotes = item.Password.Notes;
            if (newNotes != oldNotes)
            {
                changes.Add("Notes");
                item.Password.Notes = newNotes;
            }
        }

        return changes;
    }

    private async Task<List<string>> UpdateNoteAsync(VaultItem item, UpdateVaultItemDTO dto, string encryptionKey)
    {
        var changes = new List<string>();

        if (item.Note == null)
        {
            item.Note = new VaultNote { VaultItemId = item.Id };
            _dbContext.VaultNotes.Add(item.Note);
        }

        if (dto.NoteContent != null)
        {
            var newContent = _encryptionService.Encrypt(dto.NoteContent, encryptionKey);
            var oldContent = item.Note.EncryptedContent;
            if (newContent != oldContent)
            {
                changes.Add("Content");
                item.Note.EncryptedContent = newContent;
            }
        }
        if (dto.ContentFormat.HasValue && dto.ContentFormat.Value != item.Note.ContentFormat)
        {
            changes.Add("Content Format");
            item.Note.ContentFormat = dto.ContentFormat.Value;
        }

        return changes;
    }

    private async Task<List<string>> UpdateLinkAsync(VaultItem item, UpdateVaultItemDTO dto, string encryptionKey)
    {
        var changes = new List<string>();

        if (item.Link == null)
        {
            item.Link = new VaultLink { VaultItemId = item.Id };
            _dbContext.VaultLinks.Add(item.Link);
        }

        if (dto.Url != null && dto.Url != item.Link.Url)
        {
            changes.Add("URL");
            item.Link.Url = dto.Url;
        }
        if (dto.LinkNotes != null)
        {
            var newNotes = !string.IsNullOrEmpty(dto.LinkNotes) ? _encryptionService.Encrypt(dto.LinkNotes, encryptionKey) : null;
            var oldNotes = item.Link.Notes;
            if (newNotes != oldNotes)
            {
                changes.Add("Notes");
                item.Link.Notes = newNotes;
            }
        }

        return changes;
    }

    private async Task<List<string>> UpdateCryptoWalletAsync(VaultItem item, UpdateVaultItemDTO dto, string encryptionKey)
    {
        var changes = new List<string>();

        if (item.CryptoWallet == null)
        {
            item.CryptoWallet = new VaultCryptoWallet { VaultItemId = item.Id };
            _dbContext.VaultCryptoWallets.Add(item.CryptoWallet);
        }

        if (dto.WalletType.HasValue && dto.WalletType.Value != item.CryptoWallet.WalletType)
        {
            changes.Add("Wallet Type");
            item.CryptoWallet.WalletType = dto.WalletType.Value;
        }
        if (dto.PlatformName != null && dto.PlatformName != item.CryptoWallet.PlatformName)
        {
            changes.Add("Platform Name");
            item.CryptoWallet.PlatformName = dto.PlatformName;
        }
        if (dto.Blockchain != null && dto.Blockchain != item.CryptoWallet.Blockchain)
        {
            changes.Add("Blockchain");
            item.CryptoWallet.Blockchain = dto.Blockchain;
        }
        if (dto.PublicAddress != null && dto.PublicAddress != item.CryptoWallet.PublicAddress)
        {
            changes.Add("Public Address");
            item.CryptoWallet.PublicAddress = dto.PublicAddress;
        }
        if (dto.Secret != null)
        {
            changes.Add("Secret");
            item.CryptoWallet.EncryptedSecret = _encryptionService.Encrypt(dto.Secret, encryptionKey);
        }
        if (dto.CryptoNotes != null)
        {
            var newNotes = !string.IsNullOrEmpty(dto.CryptoNotes) ? _encryptionService.Encrypt(dto.CryptoNotes, encryptionKey) : null;
            var oldNotes = item.CryptoWallet.Notes;
            if (newNotes != oldNotes)
            {
                changes.Add("Notes");
                item.CryptoWallet.Notes = newNotes;
            }
        }

        return changes;
    }

    private async Task UpdateItemVisibilitiesAsync(int itemId, List<ItemVisibilityDTO> visibilities, int vaultId, string userId)
    {
        await SetItemVisibilitiesAsync(itemId, visibilities, vaultId, userId);
    }

    private async Task<ItemPermission?> GetUserPermissionForItemAsync(VaultItem item, string userId)
    {
        // Check if user is the vault owner - owners always have Edit permission
        var vault = await _dbContext.Vaults
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == item.VaultId);
        
        if (vault != null && vault.OwnerId == userId)
            return ItemPermission.Edit;

        // First, check if user is a member of the vault
        var member = await _dbContext.VaultMembers
            .FirstOrDefaultAsync(m => m.VaultId == item.VaultId && m.UserId == userId && m.Status == MemberStatus.Active);

        if (member == null)
            return null;

        // Check item-specific visibility from VaultItemVisibility table
        // This is the source of truth - all permissions come from this table
        var visibility = item.Visibilities
            .FirstOrDefault(v => v.VaultMemberId == member.Id);

        // If no visibility record exists, user cannot see the item
        if (visibility == null)
            return null;

        return visibility.Permission;
    }

    private async Task<VaultItemResponseDTO> MapToResponseDTO(VaultItem item, string userId)
    {
        var encryptionKey = await GetVaultEncryptionKeyAsync(item.VaultId, userId);
        var permission = await GetUserPermissionForItemAsync(item, userId);

        var dto = new VaultItemResponseDTO
        {
            Id = item.Id,
            VaultId = item.VaultId,
            CreatedByUserId = item.CreatedByUserId,
            CreatedByUserName = item.CreatedByUser?.UserName,
            ItemType = item.ItemType,
            Title = item.Title,
            Description = item.Description,
            Status = item.Status,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            DeletedAt = item.DeletedAt,
            DeletedBy = item.DeletedBy,
            UserPermission = permission
        };

        // Map item-specific data
        if (item.Document != null)
        {
            var downloadUrl = await _s3Service.GetPresignedUrlAsync(item.Document.ObjectKey, 60);
            dto.Document = new VaultDocumentDTO
            {
                ObjectKey = item.Document.ObjectKey,
                OriginalFileName = item.Document.OriginalFileName,
                ContentType = item.Document.ContentType,
                FileSize = item.Document.FileSize,
                UploadedAt = item.Document.UploadedAt,
                DownloadUrl = downloadUrl
            };
        }

        if (item.Password != null && permission != null)
        {
            dto.Password = new VaultPasswordDTO
            {
                Username = item.Password.Username,
                Password = !string.IsNullOrEmpty(item.Password.EncryptedPassword) ? _encryptionService.Decrypt(item.Password.EncryptedPassword, encryptionKey) : null,
                WebsiteUrl = item.Password.WebsiteUrl,
                Notes = !string.IsNullOrEmpty(item.Password.Notes) ? _encryptionService.Decrypt(item.Password.Notes, encryptionKey) : null
            };
        }

        if (item.Note != null && permission != null)
        {
            dto.Note = new VaultNoteDTO
            {
                Content = !string.IsNullOrEmpty(item.Note.EncryptedContent) ? _encryptionService.Decrypt(item.Note.EncryptedContent, encryptionKey) : string.Empty,
                ContentFormat = item.Note.ContentFormat
            };
        }

        if (item.Link != null && permission != null)
        {
            dto.Link = new VaultLinkDTO
            {
                Url = item.Link.Url,
                Notes = !string.IsNullOrEmpty(item.Link.Notes) ? _encryptionService.Decrypt(item.Link.Notes, encryptionKey) : null
            };
        }

        if (item.CryptoWallet != null && permission != null)
        {
            dto.CryptoWallet = new VaultCryptoWalletDTO
            {
                WalletType = item.CryptoWallet.WalletType,
                PlatformName = item.CryptoWallet.PlatformName,
                Blockchain = item.CryptoWallet.Blockchain,
                PublicAddress = item.CryptoWallet.PublicAddress,
                Secret = !string.IsNullOrEmpty(item.CryptoWallet.EncryptedSecret) ? _encryptionService.Decrypt(item.CryptoWallet.EncryptedSecret, encryptionKey) : null,
                Notes = !string.IsNullOrEmpty(item.CryptoWallet.Notes) ? _encryptionService.Decrypt(item.CryptoWallet.Notes, encryptionKey) : null
            };
        }

        // Map visibilities
        dto.Visibilities = item.Visibilities.Select(v => new ItemVisibilityResponseDTO
        {
            Id = v.Id,
            VaultItemId = v.VaultItemId,
            VaultMemberId = v.VaultMemberId,
            MemberEmail = v.VaultMember?.User?.Email,
            MemberName = v.VaultMember?.User?.UserName,
            Permission = v.Permission
        }).ToList();

        return dto;
    }
}

