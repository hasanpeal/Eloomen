using server.Models;

namespace server.Dtos.VaultItem;

public class VaultItemResponseDTO
{
    public int Id { get; set; }
    public int VaultId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string? CreatedByUserName { get; set; }
    public ItemType ItemType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ItemStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // Document-specific
    public VaultDocumentDTO? Document { get; set; }
    
    // Password-specific
    public VaultPasswordDTO? Password { get; set; }
    
    // Note-specific
    public VaultNoteDTO? Note { get; set; }
    
    // Link-specific
    public VaultLinkDTO? Link { get; set; }
    
    // CryptoWallet-specific
    public VaultCryptoWalletDTO? CryptoWallet { get; set; }
    
    // Visibility
    public List<ItemVisibilityResponseDTO> Visibilities { get; set; } = new();
    public ItemPermission? UserPermission { get; set; }
}

public class VaultDocumentDTO
{
    public string ObjectKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? DownloadUrl { get; set; } // Pre-signed URL for download
}

public class VaultPasswordDTO
{
    public string? Username { get; set; }
    public string? Password { get; set; } // Decrypted
    public string? WebsiteUrl { get; set; }
    public string? Notes { get; set; } // Decrypted
}

public class VaultNoteDTO
{
    public string Content { get; set; } = string.Empty; // Decrypted
    public ContentFormat ContentFormat { get; set; }
}

public class VaultLinkDTO
{
    public string Url { get; set; } = string.Empty;
    public string? Notes { get; set; } // Decrypted
}

public class VaultCryptoWalletDTO
{
    public WalletType WalletType { get; set; }
    public string? PlatformName { get; set; }
    public string? Blockchain { get; set; }
    public string? PublicAddress { get; set; }
    public string? Secret { get; set; } // Decrypted
    public string? Notes { get; set; } // Decrypted
}

public class ItemVisibilityResponseDTO
{
    public int Id { get; set; }
    public int VaultItemId { get; set; }
    public int VaultMemberId { get; set; }
    public string? MemberEmail { get; set; }
    public string? MemberName { get; set; }
    public ItemPermission Permission { get; set; }
}

