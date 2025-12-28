using System.ComponentModel.DataAnnotations;

namespace server.Dtos.Account;

public class RegisterDTO
{
    [Required]
    public string Username { get; set; }
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
    public string? InviteToken { get; set; }
}