using server.Models;

namespace server.Dtos.Vault;

public class VaultMemberResponseDTO
{
    public int Id { get; set; }
    public int VaultId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string? UserName { get; set; }
    public Privilege Privilege { get; set; }
    public MemberStatus Status { get; set; }
    public string? RemovedById { get; set; }
    public string? RemovedByEmail { get; set; }
    public string? RemovedByName { get; set; }
    public string? AddedById { get; set; }
    public string? AddedByEmail { get; set; }
    public string? AddedByName { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public DateTime? RemovedAt { get; set; }
}

