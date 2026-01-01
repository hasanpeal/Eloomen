using server.Models;

namespace server.Dtos.Vault;

public class VaultInviteResponseDTO
{
    public int Id { get; set; }
    public int VaultId { get; set; }
    public string InviterId { get; set; } = string.Empty;
    public string? InviterEmail { get; set; }
    public string InviteeEmail { get; set; } = string.Empty;
    public string? InviteeId { get; set; }
    public Privilege Privilege { get; set; }
    public InviteStatus Status { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public string? Note { get; set; }
}

