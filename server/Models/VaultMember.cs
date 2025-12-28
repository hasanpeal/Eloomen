using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("VaultMembers")]
public class VaultMember
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
    public Privilege Privilege { get; set; }
    
    [Required]
    public MemberStatus Status { get; set; } = MemberStatus.Active;
    
    public string? RemovedById { get; set; }
    
    [ForeignKey(nameof(RemovedById))]
    public User? RemovedBy { get; set; }
    
    public string? AddedById { get; set; }
    
    [ForeignKey(nameof(AddedById))]
    public User? AddedBy { get; set; }
    
    [Required]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LeftAt { get; set; }
    
    public DateTime? RemovedAt { get; set; }
}

