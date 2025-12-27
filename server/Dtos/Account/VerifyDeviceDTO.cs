using System.ComponentModel.DataAnnotations;

namespace server.Dtos.Account;

public class VerifyDeviceDTO
{
    [Required]
    public string Code { get; set; } = string.Empty;
}

