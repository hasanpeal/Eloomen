using System.ComponentModel.DataAnnotations;

namespace server.Dtos.Vault;

public class AcceptInviteDTO
{
    [Required]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

