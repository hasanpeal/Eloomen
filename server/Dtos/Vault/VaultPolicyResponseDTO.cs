using server.Models;

namespace server.Dtos.Vault;

public class VaultPolicyResponseDTO
{
    public PolicyType PolicyType { get; set; }
    public ReleaseStatus ReleaseStatus { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
}

