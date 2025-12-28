using System.ComponentModel.DataAnnotations;

namespace server.Dtos.Vault;

public class CreateVaultDTO
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
}

