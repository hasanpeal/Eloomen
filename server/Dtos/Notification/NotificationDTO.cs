namespace server.Dtos.Notification;

public class NotificationDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public int? VaultId { get; set; }
    public int? ItemId { get; set; }
    public int? InviteId { get; set; }
}

