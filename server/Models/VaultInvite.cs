using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models;

[Table("VaultInvites")]
public class VaultInvite
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int VaultId { get; set; }
    
    [ForeignKey(nameof(VaultId))]
    public Vault? Vault { get; set; }
    
    [Required]
    public string InviterId { get; set; } = string.Empty;
    
    [ForeignKey(nameof(InviterId))]
    public User? Inviter { get; set; }
    
    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string InviteeEmail { get; set; } = string.Empty;
    
    public string? InviteeId { get; set; }
    
    [ForeignKey(nameof(InviteeId))]
    public User? Invitee { get; set; }
    
    [Required]
    public Privilege Privilege { get; set; }
    
    [Required]
    public InviteType InviteType { get; set; }
    
    [Required]
    public InviteStatus Status { get; set; } = InviteStatus.Pending;
    
    public DateTime? SentAt { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    [Required]
    [MaxLength(256)]
    public string TokenHash { get; set; } = string.Empty;
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? AcceptedAt { get; set; }
    
    [MaxLength(500)]
    public string? Note { get; set; }
}

