using System.ComponentModel.DataAnnotations;
using server.Models;

namespace server.Dtos.Vault;

public class CreateVaultDTO
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public PolicyType PolicyType { get; set; }
    
    // For TimeBased policies
    public DateTime? ReleaseDate { get; set; }
    
    // For ExpiryBased policies
    public DateTime? ExpiresAt { get; set; }
}

