using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("VaultLogs")]
public class VaultLog
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int VaultId { get; set; }
    
    [ForeignKey(nameof(VaultId))]
    public Vault? Vault { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string? TargetUserId { get; set; }
    
    [ForeignKey(nameof(TargetUserId))]
    public User? TargetUser { get; set; }
    
    public int? ItemId { get; set; }
    
    [MaxLength(1000)]
    public string? AdditionalContext { get; set; }
}

