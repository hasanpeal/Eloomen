using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("VaultCryptoWallets")]
public class VaultCryptoWallet
{
    [Key]
    [ForeignKey(nameof(VaultItem))]
    public int VaultItemId { get; set; }
    
    public VaultItem? VaultItem { get; set; }
    
    [Required]
    public WalletType WalletType { get; set; }
    
    [MaxLength(200)]
    public string? PlatformName { get; set; }
    
    [MaxLength(100)]
    public string? Blockchain { get; set; }
    
    [MaxLength(500)]
    public string? PublicAddress { get; set; }
    
    [Required]
    public string EncryptedSecret { get; set; } = string.Empty;
    
    public string? Notes { get; set; } // Encrypted, nullable
}

