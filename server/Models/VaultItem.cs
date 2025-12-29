using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("VaultItems")]
public class VaultItem
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int VaultId { get; set; }
    
    [ForeignKey(nameof(VaultId))]
    public Vault? Vault { get; set; }
    
    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;
    
    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }
    
    [Required]
    public ItemType ItemType { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    [Required]
    public ItemStatus Status { get; set; } = ItemStatus.Active;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? DeletedAt { get; set; }
    
    public string? DeletedBy { get; set; }
    
    [ForeignKey(nameof(DeletedBy))]
    public User? DeletedByUser { get; set; }
    
    // Navigation properties
    public ICollection<VaultItemVisibility> Visibilities { get; set; } = new List<VaultItemVisibility>();
    
    // One-to-one relationships with specific item types
    public VaultDocument? Document { get; set; }
    public VaultPassword? Password { get; set; }
    public VaultNote? Note { get; set; }
    public VaultLink? Link { get; set; }
    public VaultCryptoWallet? CryptoWallet { get; set; }
}

