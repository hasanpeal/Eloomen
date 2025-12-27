using System.ComponentModel.DataAnnotations;

namespace server.Dtos.Account;

public class VerifyEmailDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Code { get; set; } = string.Empty;
}

