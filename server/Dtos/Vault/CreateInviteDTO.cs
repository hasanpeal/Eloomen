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
    
    [Required]
    public InviteType InviteType { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    [MaxLength(500)]
    public string? Note { get; set; }
}

