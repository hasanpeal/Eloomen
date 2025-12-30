using System.ComponentModel.DataAnnotations;
using server.Models;

namespace server.Dtos.Vault;

public class CreateInviteDTO
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string InviteeEmail { get; set; } = string.Empty;
    
    [Required]
    public Privilege Privilege { get; set; }
    
    // Invite expiration (when the invite itself expires)
    public DateTime? InviteExpiresAt { get; set; }
    
    [MaxLength(500)]
    public string? Note { get; set; }
}

