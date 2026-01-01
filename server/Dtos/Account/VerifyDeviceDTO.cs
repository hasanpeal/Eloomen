using System.ComponentModel.DataAnnotations;

namespace server.Dtos.Account;

public class VerifyDeviceDTO
{
    [Required]
    public string UsernameOrEmail { get; set; } = string.Empty;
    
    [Required]
    public string Code { get; set; } = string.Empty;
    
    public string? InviteToken { get; set; }
}

