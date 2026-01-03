using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using server.Controllers;
using server.Dtos.Notification;
using server.Interfaces;
using server.Models;
using System.Security.Claims;

namespace server.Tests.Controllers;

public class NotificationControllerTests
{
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly NotificationController _controller;
    private readonly string _testUserId = Guid.NewGuid().ToString();

    public NotificationControllerTests()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _controller = new NotificationController(_notificationServiceMock.Object);
        
        // Setup controller context with user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId),
            new Claim("sub", _testUserId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [Fact]
    public async Task GetNotifications_ReturnsNotifications()
    {
        // Arrange
        var notifications = new List<NotificationDTO>
        {
            new NotificationDTO { Id = 1, Title = "Test 1", IsRead = false },
            new NotificationDTO { Id = 2, Title = "Test 2", IsRead = true }
        };
        _notificationServiceMock.Setup(x => x.GetUserNotificationsAsync(_testUserId, false))
            .ReturnsAsync(notifications);

        // Act
        var result = await _controller.GetNotifications();

        // Assert
        var okResult = Assert.IsType<ActionResult<List<NotificationDTO>>>(result);
        var okObjectResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var returnedNotifications = Assert.IsType<List<NotificationDTO>>(okObjectResult.Value);
        Assert.Equal(2, returnedNotifications.Count);
    }

    [Fact]
    public async Task GetNotifications_WithUnreadOnly_ReturnsOnlyUnread()
    {
        // Arrange
        var notifications = new List<NotificationDTO>
        {
            new NotificationDTO { Id = 1, Title = "Unread", IsRead = false }
        };
        _notificationServiceMock.Setup(x => x.GetUserNotificationsAsync(_testUserId, true))
            .ReturnsAsync(notifications);

        // Act
        var result = await _controller.GetNotifications(unreadOnly: true);

        // Assert
        var okResult = Assert.IsType<ActionResult<List<NotificationDTO>>>(result);
        var okObjectResult = Assert.IsType<OkObjectResult>(okResult.Result);
        var returnedNotifications = Assert.IsType<List<NotificationDTO>>(okObjectResult.Value);
        Assert.Single(returnedNotifications);
    }

    [Fact]
    public async Task MarkAsRead_WithValidId_ReturnsOk()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.MarkNotificationAsReadAsync(1, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.MarkAsRead(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task MarkAsRead_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.MarkNotificationAsReadAsync(999, _testUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.MarkAsRead(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteNotification_WithValidId_ReturnsOk()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.DeleteNotificationAsync(1, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteNotification(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteNotification_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.DeleteNotificationAsync(999, _testUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteNotification(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAllRead_ReturnsCount()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.DeleteAllReadNotificationsAsync(_testUserId))
            .ReturnsAsync(5);

        // Act
        var result = await _controller.DeleteAllRead();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        Assert.NotNull(value);
    }
}

