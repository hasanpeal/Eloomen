using System.ComponentModel.DataAnnotations;

namespace server.Dtos.Account;

public class ForgotPasswordDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

