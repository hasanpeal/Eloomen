using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Dtos.Notification;
using server.Interfaces;

namespace server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationDTO>>> GetNotifications([FromQuery] bool unreadOnly = false)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                     User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
        return Ok(notifications);
    }

    [HttpPost("{id}/read")]
    public async Task<ActionResult> MarkAsRead(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                     User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _notificationService.MarkNotificationAsReadAsync(id, userId);
        if (!result)
        {
            return NotFound();
        }

        return Ok(new { message = "Notification marked as read" });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNotification(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                     User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _notificationService.DeleteNotificationAsync(id, userId);
        if (!result)
        {
            return NotFound();
        }

        return Ok(new { message = "Notification deleted" });
    }

    [HttpDelete("read")]
    public async Task<ActionResult> DeleteAllRead()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                     User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var count = await _notificationService.DeleteAllReadNotificationsAsync(userId);
        return Ok(new { message = $"Deleted {count} read notification(s)" });
    }
}

