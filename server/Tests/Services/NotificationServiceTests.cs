using Microsoft.EntityFrameworkCore;
using server.Models;
using server.Services;
using server.Tests.Helpers;

namespace server.Tests.Services;

public class NotificationServiceTests : IDisposable
{
    private readonly ApplicationDBContext _dbContext;
    private readonly NotificationService _notificationService;
    private readonly User _testUser;

    public NotificationServiceTests()
    {
        _dbContext = TestHelpers.CreateInMemoryDbContext();
        _notificationService = new NotificationService(_dbContext);
        
        _testUser = TestHelpers.CreateTestUser();
        _dbContext.Users.Add(_testUser);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ReturnsAllNotifications()
    {
        // Arrange
        var notification1 = new Notification
        {
            UserId = _testUser.Id,
            Title = "Test 1",
            Description = "Description 1",
            Type = "Test",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        var notification2 = new Notification
        {
            UserId = _testUser.Id,
            Title = "Test 2",
            Description = "Description 2",
            Type = "Test",
            IsRead = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        _dbContext.Notifications.AddRange(notification1, notification2);
        await _dbContext.SaveChangesAsync();

        // Act
        var notifications = await _notificationService.GetUserNotificationsAsync(_testUser.Id);

        // Assert
        Assert.Equal(2, notifications.Count);
        Assert.Equal("Test 1", notifications[0].Title); // Should be ordered by CreatedAt descending
    }

    [Fact]
    public async Task GetUserNotificationsAsync_WithUnreadOnly_ReturnsOnlyUnread()
    {
        // Arrange
        var notification1 = new Notification
        {
            UserId = _testUser.Id,
            Title = "Unread",
            Description = "Description",
            Type = "Test",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        var notification2 = new Notification
        {
            UserId = _testUser.Id,
            Title = "Read",
            Description = "Description",
            Type = "Test",
            IsRead = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        _dbContext.Notifications.AddRange(notification1, notification2);
        await _dbContext.SaveChangesAsync();

        // Act
        var notifications = await _notificationService.GetUserNotificationsAsync(_testUser.Id, unreadOnly: true);

        // Assert
        Assert.Single(notifications);
        Assert.Equal("Unread", notifications[0].Title);
        Assert.False(notifications[0].IsRead);
    }

    [Fact]
    public async Task MarkNotificationAsReadAsync_WithValidNotification_MarksAsRead()
    {
        // Arrange
        var notification = new Notification
        {
            UserId = _testUser.Id,
            Title = "Test",
            Description = "Description",
            Type = "Test",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _notificationService.MarkNotificationAsReadAsync(notification.Id, _testUser.Id);

        // Assert
        Assert.True(result);
        var updatedNotification = await _dbContext.Notifications.FindAsync(notification.Id);
        Assert.NotNull(updatedNotification);
        Assert.True(updatedNotification.IsRead);
        Assert.NotNull(updatedNotification.ReadAt);
    }

    [Fact]
    public async Task MarkNotificationAsReadAsync_WithInvalidId_ReturnsFalse()
    {
        // Act
        var result = await _notificationService.MarkNotificationAsReadAsync(999, _testUser.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteNotificationAsync_WithValidNotification_Deletes()
    {
        // Arrange
        var notification = new Notification
        {
            UserId = _testUser.Id,
            Title = "Test",
            Description = "Description",
            Type = "Test",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _notificationService.DeleteNotificationAsync(notification.Id, _testUser.Id);

        // Assert
        Assert.True(result);
        var deleted = await _dbContext.Notifications.FindAsync(notification.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAllReadNotificationsAsync_DeletesOnlyRead()
    {
        // Arrange
        var read1 = new Notification { UserId = _testUser.Id, Title = "Read 1", IsRead = true, CreatedAt = DateTime.UtcNow, Type = "Test", Description = "Desc" };
        var read2 = new Notification { UserId = _testUser.Id, Title = "Read 2", IsRead = true, CreatedAt = DateTime.UtcNow, Type = "Test", Description = "Desc" };
        var unread = new Notification { UserId = _testUser.Id, Title = "Unread", IsRead = false, CreatedAt = DateTime.UtcNow, Type = "Test", Description = "Desc" };
        _dbContext.Notifications.AddRange(read1, read2, unread);
        await _dbContext.SaveChangesAsync();

        // Act
        var count = await _notificationService.DeleteAllReadNotificationsAsync(_testUser.Id);

        // Assert
        Assert.Equal(2, count);
        var remaining = await _dbContext.Notifications.Where(n => n.UserId == _testUser.Id).ToListAsync();
        Assert.Single(remaining);
        Assert.Equal("Unread", remaining[0].Title);
    }

    [Fact]
    public async Task CreateNotificationAsync_CreatesNotification()
    {
        // Act
        await _notificationService.CreateNotificationAsync(
            _testUser.Id,
            "Test Title",
            "Test Description",
            "TestType",
            vaultId: 1,
            itemId: 2,
            inviteId: 3
        );

        // Assert
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.UserId == _testUser.Id && n.Title == "Test Title");
        Assert.NotNull(notification);
        Assert.Equal("Test Description", notification.Description);
        Assert.Equal("TestType", notification.Type);
        Assert.Equal(1, notification.VaultId);
        Assert.Equal(2, notification.ItemId);
        Assert.Equal(3, notification.InviteId);
        Assert.False(notification.IsRead);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

