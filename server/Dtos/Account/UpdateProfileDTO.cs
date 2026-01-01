using System.ComponentModel.DataAnnotations;

namespace server.Dtos.Account;

public class UpdateProfileDTO
{
    [MaxLength(256)]
    public string? Username { get; set; }
    
    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }
}

