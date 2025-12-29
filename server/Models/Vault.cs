using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("Vaults")]
public class Vault
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string OwnerId { get; set; } = string.Empty;
    
    [ForeignKey(nameof(OwnerId))]
    public User? Owner { get; set; }
    
    [Required]
    public string OriginalOwnerId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public VaultStatus Status { get; set; } = VaultStatus.Active;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public ICollection<VaultMember> Members { get; set; } = new List<VaultMember>();
    public ICollection<VaultInvite> Invites { get; set; } = new List<VaultInvite>();
    public ICollection<VaultItem> Items { get; set; } = new List<VaultItem>();
}

