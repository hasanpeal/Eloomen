using System.ComponentModel.DataAnnotations;

namespace server.Dtos.Account;

public class LoginDTO
{
    [Required]
    public string UsernameOrEmail { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
    public string? InviteToken { get; set; }
}