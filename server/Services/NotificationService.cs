using Microsoft.EntityFrameworkCore;
using server.Dtos.Notification;
using server.Interfaces;
using server.Models;

namespace server.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDBContext _dbContext;

    public NotificationService(ApplicationDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<NotificationDTO>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
    {
        var query = _dbContext.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(n => new NotificationDTO
        {
            Id = n.Id,
            Title = n.Title,
            Description = n.Description,
            Type = n.Type,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            ReadAt = n.ReadAt,
            VaultId = n.VaultId,
            ItemId = n.ItemId,
            InviteId = n.InviteId
        }).ToList();
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId, string userId)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId, string userId)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return false;

        _dbContext.Notifications.Remove(notification);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<int> DeleteAllReadNotificationsAsync(string userId)
    {
        var readNotifications = await _dbContext.Notifications
            .Where(n => n.UserId == userId && n.IsRead)
            .ToListAsync();

        var count = readNotifications.Count;
        _dbContext.Notifications.RemoveRange(readNotifications);
        await _dbContext.SaveChangesAsync();

        return count;
    }

    public async Task CreateNotificationAsync(string userId, string title, string description, string type, int? vaultId = null, int? itemId = null, int? inviteId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Description = description,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            VaultId = vaultId,
            ItemId = itemId,
            InviteId = inviteId
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();
    }
}

