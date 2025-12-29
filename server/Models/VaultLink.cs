using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("VaultLinks")]
public class VaultLink
{
    [Key]
    [ForeignKey(nameof(VaultItem))]
    public int VaultItemId { get; set; }
    
    public VaultItem? VaultItem { get; set; }
    
    [Required]
    [MaxLength(2000)]
    public string Url { get; set; } = string.Empty;
    
    public string? Notes { get; set; } // Encrypted, nullable
}

