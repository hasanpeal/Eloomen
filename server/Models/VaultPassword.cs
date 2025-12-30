using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("VaultPasswords")]
public class VaultPassword
{
    [Key]
    [ForeignKey(nameof(VaultItem))]
    public int VaultItemId { get; set; }
    
    public VaultItem? VaultItem { get; set; }
    
    [MaxLength(500)]
    public string? Username { get; set; }
    
    [Required]
    public string EncryptedPassword { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? WebsiteUrl { get; set; }
    
    public string? Notes { get; set; } // Encrypted, nullable
}

