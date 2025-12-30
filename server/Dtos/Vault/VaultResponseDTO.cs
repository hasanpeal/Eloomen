using server.Models;

namespace server.Dtos.Vault;

public class VaultResponseDTO
{
    public int Id { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string? OwnerEmail { get; set; }
    public string? OwnerName { get; set; }
    public string OriginalOwnerId { get; set; } = string.Empty;
    public string? OriginalOwnerEmail { get; set; }
    public string? OriginalOwnerName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public VaultStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Privilege? UserPrivilege { get; set; }
    public VaultPolicyResponseDTO? Policy { get; set; }
}

