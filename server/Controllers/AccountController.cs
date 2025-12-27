using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Dtos.Account;
using server.Interfaces;
using server.Models;

namespace server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IDeviceService _deviceService;
    private readonly IConfiguration _config;
    private readonly ApplicationDBContext _dbContext;
    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, ITokenService tokenService, IDeviceService deviceService, IConfiguration config, ApplicationDBContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _deviceService = deviceService;
        _config = config;
        _dbContext = dbContext;
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] RegisterDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _userManager.FindByEmailAsync(dto.Email) != null ||
                await _userManager.FindByNameAsync(dto.Username) != null)
            {
                return BadRequest("User already exists");
            }
            var user = new User()
            {
                UserName = dto.Username,
                Email = dto.Email,
                EmailConfirmed = false,
            };
            
            // Password hashing is handled by userManager
            var createdUser = await _userManager.CreateAsync(user, dto.Password);
            var deviceIdentifier = HttpContext.Request.Headers["X-Device-Id"].ToString();
            
            if (createdUser.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, "User");
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                // TO BE IMPLEMENTED: Sent confirmation email
                
                var device = await _deviceService.GetOrCreateDeviceAsync(user, deviceIdentifier);
                await _dbContext.SaveChangesAsync(); // Save device if it was newly created
                
                if (roleResult.Succeeded)
                {
                    return Ok(
                        new
                        {
                            RequireVerification = true,
                            Message = "Check your email for verification.",
                        });
                }
                else
                {
                    return StatusCode(500, roleResult.Errors.Select(x => x.Description).ToArray());
                }
            }
            else
            {
                return StatusCode(500, createdUser.Errors.Select(x => x.Description).ToArray());
            }
        }
        catch (Exception e)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var user = await _userManager.FindByNameAsync(dto.Username);
        if(user == null) return Unauthorized("Username or password is incorrect");
        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, true);
        if(!result.Succeeded) return Unauthorized("Username or password is incorrect");
        
        // Extract unique device identifier
        string deviceIdentifier = HttpContext.Request.Headers["X-Device-Id"].ToString();
        var device = await _deviceService.GetOrCreateDeviceAsync(user, deviceIdentifier);
        await _dbContext.SaveChangesAsync(); // Save device if it was newly created
        
        if (!device.IsVerified)
        {
            var token = await _userManager.GenerateTwoFactorTokenAsync(user, _config["Jwt:Issuer"] + "-Device-" + deviceIdentifier);
            // TO BE IMPLEMENTED: Email send logic for device verification
            return Ok(new
            {
                RequireVerification = true,
                Message = "New device detected. Check your email and verify."
            });
        }
        // Email verification check
        if (!user.EmailConfirmed)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            // TO BE IMPLEMENTED: Email send logic for email verification
            return Ok(new
            {
                RequireVerification = true,
                Message = "Email not verified. Check your email and verify."    
            });
        }
        // Account lockout check
        if (user.LockoutEnabled && user.LockoutEnd.HasValue)
        {
            return Unauthorized($"Account is locked out. Lockout end: {user.LockoutEnd.Value.ToLocalTime()}");
        }

        var refreshTokenValue = _tokenService.CreateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserDeviceId = device.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenDays"])),
            Revoked = false
        };
        
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();
        SetRefreshCookie(refreshToken);
        
        return Ok(
            new
            {
                RequireVerification = false,
                UserName = user.UserName,
                Email = user.Email,
                Token = _tokenService.CreateToken(user)
            }
        );
    }
    
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshTokenValue = Request.Cookies["refreshToken"];
        if (refreshTokenValue == null) return Unauthorized();

        var refreshToken = await _dbContext.RefreshTokens
            .Include(r => r.UserDevice)
            .ThenInclude(d => d.User)
            .FirstOrDefaultAsync(r =>
                r.Token == refreshTokenValue &&
                !r.Revoked &&
                r.ExpiresAt > DateTime.UtcNow
            );

        if (refreshToken == null) return Unauthorized();

        // Validate user account state
        var user = refreshToken.UserDevice.User;
        if (user == null) return Unauthorized();
        
        if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            return Unauthorized("Account is locked out");
        }
        
        if (!user.EmailConfirmed)
        {
            return Unauthorized("Email not verified");
        }
        
        if (!refreshToken.UserDevice.IsVerified)
        {
            return Unauthorized("Device not verified");
        }

        refreshToken.Revoked = true;

        var newTokenValue = _tokenService.CreateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            Token = newTokenValue,
            UserDeviceId = refreshToken.UserDeviceId,
            ExpiresAt = DateTime.UtcNow.AddDays(
                int.Parse(_config["Jwt:RefreshTokenDays"]!)
            ),
            Revoked = false
        };

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync();

        SetRefreshCookie(newRefreshToken);

        return Ok(new
        {
            Token = _tokenService.CreateToken(user)
        });
    }
    
    // Helper function to save refresh token on http cookies
    private void SetRefreshCookie(RefreshToken token)
    {
        Response.Cookies.Append("refreshToken", token.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = token.ExpiresAt
        });
    }
}