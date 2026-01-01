namespace server.Dtos.Vault;

public class VaultLogResponseDTO
{
    public int Id { get; set; }
    public int VaultId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string? UserName { get; set; }
    public string? TargetUserId { get; set; }
    public string? TargetUserEmail { get; set; }
    public string? TargetUserName { get; set; }
    public int? ItemId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? AdditionalContext { get; set; }
}

