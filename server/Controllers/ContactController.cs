using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using server.Dtos.Contact;
using server.Interfaces;
using server.Models;

namespace server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;

    public ContactController(UserManager<User> userManager, IEmailService emailService)
    {
        _userManager = userManager;
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<ActionResult> SendContact([FromBody] ContactRequestDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            await _emailService.SendContactEmailAsync(
                user.UserName ?? "Unknown",
                user.Email ?? "Unknown",
                user.Id,
                dto.Name,
                dto.Message
            );

            return Ok(new { message = "Contact message sent successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to send contact message. Please try again later." });
        }
    }
}

