namespace server.Dtos.Vault;

public class InviteInfoDTO
{
    public string InviteeEmail { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public bool UserExists { get; set; }
}

