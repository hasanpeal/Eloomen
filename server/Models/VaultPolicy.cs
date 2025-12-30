using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("VaultPolicies")]
public class VaultPolicy
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int VaultId { get; set; }
    
    [ForeignKey(nameof(VaultId))]
    public Vault? Vault { get; set; }
    
    [Required]
    public PolicyType PolicyType { get; set; }
    
    [Required]
    public ReleaseStatus ReleaseStatus { get; set; } = ReleaseStatus.Pending;
    
    // For TimeBased policies
    public DateTime? ReleaseDate { get; set; }
    
    // For ExpiryBased policies
    public DateTime? ExpiresAt { get; set; }
    
    // When the vault was actually released
    public DateTime? ReleasedAt { get; set; }
    
    // For ManualRelease - who released it
    public string? ReleasedById { get; set; }
    
    [ForeignKey(nameof(ReleasedById))]
    public User? ReleasedBy { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(500)]
    public string? Note { get; set; }
}

