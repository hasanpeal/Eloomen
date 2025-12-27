using System.ComponentModel.DataAnnotations;

namespace server.Dtos.Account;

public class ResetPasswordDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

