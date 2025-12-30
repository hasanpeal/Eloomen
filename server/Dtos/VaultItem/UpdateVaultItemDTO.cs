using System.ComponentModel.DataAnnotations;
using server.Models;

namespace server.Dtos.VaultItem;

public class UpdateVaultItemDTO
{
    [MaxLength(500)]
    public string? Title { get; set; }
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    // Document-specific
    public IFormFile? DocumentFile { get; set; }
    public bool? DeleteDocument { get; set; }
    
    // Password-specific
    public string? Username { get; set; }
    public string? Password { get; set; } // Will be encrypted
    public string? WebsiteUrl { get; set; }
    public string? PasswordNotes { get; set; } // Will be encrypted
    
    // Note-specific
    public string? NoteContent { get; set; } // Will be encrypted
    public ContentFormat? ContentFormat { get; set; }
    
    // Link-specific
    public string? Url { get; set; }
    public string? LinkNotes { get; set; } // Will be encrypted
    
    // CryptoWallet-specific
    public WalletType? WalletType { get; set; }
    public string? PlatformName { get; set; }
    public string? Blockchain { get; set; }
    public string? PublicAddress { get; set; }
    public string? Secret { get; set; } // Will be encrypted
    public string? CryptoNotes { get; set; } // Will be encrypted
    
    // Visibility settings
    public List<ItemVisibilityDTO>? Visibilities { get; set; }
}

