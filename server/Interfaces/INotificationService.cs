using server.Dtos.Notification;

namespace server.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDTO>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
    Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId);
    Task<bool> DeleteNotificationAsync(int notificationId, string userId);
    Task<int> DeleteAllReadNotificationsAsync(string userId);
    Task CreateNotificationAsync(string userId, string title, string description, string type, int? vaultId = null, int? itemId = null, int? inviteId = null);
}

