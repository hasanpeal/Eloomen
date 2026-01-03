using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
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
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ApplicationDBContext _dbContext;
    private readonly IVaultService _vaultService;
    private readonly INotificationService _notificationService;
    
    public AccountController(
        UserManager<User> userManager, 
        SignInManager<User> signInManager, 
        ITokenService tokenService, 
        IDeviceService deviceService,
        IEmailService emailService,
        IConfiguration config, 
        ApplicationDBContext dbContext,
        IVaultService vaultService,
        INotificationService notificationService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _deviceService = deviceService;
        _emailService = emailService;
        _config = config;
        _dbContext = dbContext;
        _vaultService = vaultService;
        _notificationService = notificationService;
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] RegisterDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _userManager.FindByEmailAsync(dto.Email) != null ||
            await _userManager.FindByNameAsync(dto.Username) != null)
        {
            return BadRequest("Account already exists");
        }

        // Check if username matches any existing email
        var existingUserWithEmailAsUsername = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.Equals(dto.Username, StringComparison.OrdinalIgnoreCase));
        
        if (existingUserWithEmailAsUsername != null)
        {
            return BadRequest("Username taken");
        }

        var user = new User
        {
            UserName = dto.Username,
            Email = dto.Email,
            EmailConfirmed = false
        };

        // Create user (Identity handles persistence)
        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors.Select(e => e.Description));

        // Add role
        var roleResult = await _userManager.AddToRoleAsync(user, "User");
        if (!roleResult.Succeeded)
            return BadRequest(roleResult.Errors.Select(e => e.Description));

        // Device creation
        var deviceIdentifier = GetOrCreateDeviceId();
        await _deviceService.GetOrCreateDeviceAsync(user.Id, deviceIdentifier);
        
        await _dbContext.SaveChangesAsync();

        // Reload user after possible changes
        user = await _userManager.FindByIdAsync(user.Id);

        // Generate verification code
        var emailVerificationExpiration = int.Parse(_config["App:VerificationCodeExpiration:EmailVerificationMinutes"]);
        var code = await CreateAndStoreVerificationCodeAsync(user.Id, "EmailVerification", emailVerificationExpiration);

        // Accept invite if provided
        if (!string.IsNullOrEmpty(dto.InviteToken))
        {
            try
            {
                await _vaultService.AcceptInviteAsync(dto.InviteToken, dto.Email, user.Id);
            }
            catch
            {
                // Log but don't fail registration if invite acceptance fails
                // User can accept invite later
            }
        }

        // Send email
        try
        {
            var baseUrl = _config["App:BaseUrl"];
            var verificationPath = _config["App:EmailVerificationPath"];
            var verificationUrl = $"{baseUrl}{verificationPath}";

            await _emailService.SendEmailConfirmationAsync(
                user.Email!,
                user.UserName!,
                code,
                verificationUrl
            );
        }
        catch
        {
            return StatusCode(500, "Failed to send email");
        }

        // Log account activity
        var accountLog = new AccountLog
        {
            UserId = user.Id,
            Action = "Register",
            Timestamp = DateTime.UtcNow,
            AdditionalContext = "Account created successfully"
        };
        _dbContext.AccountLogs.Add(accountLog);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            requireVerification = true,
            verificationType = "Email",
            message = "Verification email sent",
            inviteAccepted = !string.IsNullOrEmpty(dto.InviteToken)
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        // Try to find user by username or email
        User? user = null;
        
        // First, try to find by username
        user = await _userManager.FindByNameAsync(dto.UsernameOrEmail);
        
        // If not found by username, try to find by email
        if (user == null)
        {
            user = await _userManager.FindByEmailAsync(dto.UsernameOrEmail);
        }
        
        if(user == null) return Unauthorized("Login credentials are incorrect");
        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, true);
        if(!result.Succeeded) return Unauthorized("Login credentials are incorrect");
        
        // Get or generate device identifier (backend-controlled, stored in secure cookie)
        string deviceIdentifier = GetOrCreateDeviceId();
        var device = await _deviceService.GetOrCreateDeviceAsync(user.Id, deviceIdentifier);
        await _dbContext.SaveChangesAsync(); // Save device if it was newly created
        
        if (!device.IsVerified)
        {
            // Generate device verification code
            var deviceVerificationExpiration = int.Parse(_config["App:VerificationCodeExpiration:DeviceVerificationMinutes"]);
            var code = await CreateAndStoreVerificationCodeAsync(user.Id, "DeviceVerification", deviceVerificationExpiration);
            // Send device verification email
            try
            {
                var baseUrl = _config["App:BaseUrl"];
                var verificationPath = _config["App:DeviceVerificationPath"];
                var verificationUrl = $"{baseUrl}{verificationPath}";
                await _emailService.SendDeviceVerificationAsync(user.Email!, user.UserName!, code, verificationUrl);
            }
            catch
            {
                return StatusCode(500, "Failed to send email");
            }
            
            return Ok(new
            {
                requireVerification = true,
                verificationType = "Device",
                message = "Device verification required"
            });
        }
        // Email verification check
        if (!user.EmailConfirmed)
        {
            // Generate email verification code
            var emailVerificationExpiration = int.Parse(_config["App:VerificationCodeExpiration:EmailVerificationMinutes"]);
            var code = await CreateAndStoreVerificationCodeAsync(user.Id, "EmailVerification", emailVerificationExpiration);
            // Send email verification
            try
            {
                var baseUrl = _config["App:BaseUrl"];
                var verificationPath = _config["App:EmailVerificationPath"];
                var verificationUrl = $"{baseUrl}{verificationPath}";
                await _emailService.SendEmailConfirmationAsync(user.Email!, user.UserName!, code, verificationUrl);
            }
            catch
            {
                return StatusCode(500, "Failed to send email");
            }
            
            return Ok(new
            {
                requireVerification = true,
                verificationType = "Email",
                message = "Email verification required"    
            });
        }
        // Account lockout check
        if (user.LockoutEnabled && user.LockoutEnd.HasValue)
        {
            return Unauthorized($"Account is locked out. Lockout end: {user.LockoutEnd.Value.ToLocalTime()}");
        }

        // Always generate refresh token
        var refreshTokenValue = _tokenService.CreateRefreshToken();
        DateTime expiresAt;
        
        if (dto.RememberMe)
        {
            // Persistent cookie: expires in configured days
            expiresAt = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenDays"]));
        }
        else
        {
            // Session cookie: 24 hours in database for security, but cookie has no Expires (browser deletes on close)
            expiresAt = DateTime.UtcNow.AddHours(24);
        }
        
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserDeviceId = device.Id,
            ExpiresAt = expiresAt,
            Revoked = false
        };
        
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();
        SetRefreshCookie(refreshToken, dto.RememberMe);

        // Accept invite if provided
        if (!string.IsNullOrEmpty(dto.InviteToken))
        {
            try
            {
                await _vaultService.AcceptInviteAsync(dto.InviteToken, user.Email!, user.Id);
            }
            catch
            {
                // Log but don't fail login if invite acceptance fails
                // User can accept invite later
            }
        }

        // Log account activity
        var accountLog = new AccountLog
        {
            UserId = user.Id,
            Action = "Login",
            Timestamp = DateTime.UtcNow,
            AdditionalContext = $"Logged in from device: {device.DeviceIdentifier}"
        };
        _dbContext.AccountLogs.Add(accountLog);
        await _dbContext.SaveChangesAsync();
        
        return Ok(
            new
            {
                requireVerification = false,
                userName = user.UserName,
                email = user.Email,
                token = _tokenService.CreateToken(user),
                inviteAccepted = !string.IsNullOrEmpty(dto.InviteToken)
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
        
        // Determine expiration based on original token
        // If original expires in more than 24 hours, it was a "Remember Me" token
        DateTime newExpiresAt;
        bool isPersistent;
        if (refreshToken.ExpiresAt > DateTime.UtcNow.AddHours(24))
        {
            // Original was persistent, keep it persistent
            newExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenDays"]!));
            isPersistent = true;
        }
        else
        {
            // Original was session cookie, keep it as session cookie
            newExpiresAt = DateTime.UtcNow.AddHours(24);
            isPersistent = false;
        }

        var newRefreshToken = new RefreshToken
        {
            Token = newTokenValue,
            UserDeviceId = refreshToken.UserDeviceId,
            ExpiresAt = newExpiresAt,
            Revoked = false
        };

        _dbContext.RefreshTokens.Add(newRefreshToken);
        await _dbContext.SaveChangesAsync();

        SetRefreshCookie(newRefreshToken, isPersistent);

        return Ok(new
        {
            Token = _tokenService.CreateToken(user)
        });
    }
    
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return BadRequest("Invalid verification code");
        }

        if (user.EmailConfirmed)
        {
            return Ok(new { Message = "Email already verified" });
        }
        
        var verificationCode = await VerifyCodeAsync(user.Id, dto.Code, "EmailVerification");
        if (verificationCode != null)
        {
            // Confirm email directly
            user.EmailConfirmed = true;
            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                // Also verify the device if it exists and isn't verified yet
                // This handles the case where user registered and is verifying from the same device
                var deviceIdentifier = GetOrCreateDeviceId();
                var device = await _deviceService.GetOrCreateDeviceAsync(user.Id, deviceIdentifier);
                
                if (device != null && !device.IsVerified)
                {
                    device.IsVerified = true;
                    device.VerifiedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                // Log account activity
                var accountLog = new AccountLog
                {
                    UserId = user.Id,
                    Action = "VerifyEmail",
                    Timestamp = DateTime.UtcNow
                };
                _dbContext.AccountLogs.Add(accountLog);
                await _dbContext.SaveChangesAsync();
                
                return Ok(new { Message = "Email verified" });
            }
            
            return StatusCode(500, "Failed to confirm email");
        }

        return BadRequest("Invalid verification code");
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ForgotPasswordDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            // Don't reveal if email exists for security
            return Ok(new { Message = "Verification email sent if account exists" });
        }

        if (user.EmailConfirmed)
        {
            return Ok(new { Message = "Email already verified" });
        }

        // Generate new verification code
        var emailVerificationExpiration = int.Parse(_config["App:VerificationCodeExpiration:EmailVerificationMinutes"]);
        var code = await CreateAndStoreVerificationCodeAsync(user.Id, "EmailVerification", emailVerificationExpiration);
        try
        {
            var baseUrl = _config["App:BaseUrl"];
            var verificationPath = _config["App:EmailVerificationPath"];
            var verificationUrl = $"{baseUrl}{verificationPath}";
            await _emailService.SendEmailConfirmationAsync(user.Email!, user.UserName!, code, verificationUrl);
        }
        catch
        {
            return StatusCode(500, "Failed to send email");
        }

        return Ok(new { Message = "Verification email sent if account exists" });
    }

    [HttpPost("verify-device")]
    public async Task<IActionResult> VerifyDevice([FromBody] VerifyDeviceDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Find user by username or email
        User? user = null;
        
        // First, try to find by username
        user = await _userManager.FindByNameAsync(dto.UsernameOrEmail);
        
        // If not found by username, try to find by email
        if (user == null)
        {
            user = await _userManager.FindByEmailAsync(dto.UsernameOrEmail);
        }

        if (user == null)
        {
            return BadRequest("Account not found");
        }

        // Get device identifier
        var deviceIdentifier = GetOrCreateDeviceId();
        
        // Find the device for this user and device identifier
        var device = await _dbContext.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == user.Id && d.DeviceIdentifier == deviceIdentifier);

        if (device == null)
        {
            return BadRequest("Device not found");
        }

        if (device.IsVerified)
        {
            return Ok(new { Message = "Device already verified" });
        }

        var verificationCode = await VerifyCodeAsync(user.Id, dto.Code, "DeviceVerification");

        if (verificationCode == null)
        {
            return BadRequest("Invalid verification code");
        }

        // Verify the device
        device.IsVerified = true;
        device.VerifiedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        // Log account activity
        var accountLog = new AccountLog
        {
            UserId = user.Id,
            Action = "VerifyDevice",
            Timestamp = DateTime.UtcNow,
            AdditionalContext = $"Device verified: {device.DeviceIdentifier}"
        };
        _dbContext.AccountLogs.Add(accountLog);
        await _dbContext.SaveChangesAsync();

        // Handle invite acceptance if token is provided
        bool inviteAccepted = false;
        if (!string.IsNullOrEmpty(dto.InviteToken))
        {
            try
            {
                inviteAccepted = await _vaultService.AcceptInviteAsync(dto.InviteToken, user.Email!, user.Id);
            }
            catch
            {
                // Continue even if invite acceptance fails - device is still verified
            }
        }

        // Generate access token for the user
        return Ok(new
        {
            Message = "Device verified successfully",
            UserName = user.UserName,
            Email = user.Email,
            Token = _tokenService.CreateToken(user),
            InviteAccepted = inviteAccepted
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            // Don't reveal if email exists for security
            return Ok(new { Message = "Reset code sent if account exists" });
        }

        // Generate password reset code
        var passwordResetExpiration = int.Parse(_config["App:VerificationCodeExpiration:PasswordResetMinutes"]);
        var code = await CreateAndStoreVerificationCodeAsync(user.Id, "PasswordReset", passwordResetExpiration);
        try
        {
            var baseUrl = _config["App:BaseUrl"];
            var resetPath = _config["App:PasswordResetPath"];
            var resetUrl = $"{baseUrl}{resetPath}";
            await _emailService.SendPasswordResetAsync(user.Email!, user.UserName!, code, resetUrl);
        }
        catch
        {
            return StatusCode(500, "Failed to send email");
        }

        // Log account activity
        var accountLog = new AccountLog
        {
            UserId = user.Id,
            Action = "ForgotPassword",
            Timestamp = DateTime.UtcNow
        };
        _dbContext.AccountLogs.Add(accountLog);
        await _dbContext.SaveChangesAsync();

        return Ok(new { Message = "Reset code sent if account exists" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return BadRequest("Invalid reset code");
        }

        // Verify code but don't mark as used yet
        var codeHash = HashCode(dto.Code);
        var verificationCode = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(vc => 
                vc.UserId == user.Id && 
                vc.CodeHash == codeHash && 
                vc.Purpose == "PasswordReset" &&
                !vc.IsUsed &&
                vc.ExpiresAt > DateTime.UtcNow &&
                vc.Attempts <= 5);
        
        if (verificationCode == null)
        {
            // Increment attempts for failed verification
            var userCodes = await _dbContext.VerificationCodes
                .Where(vc => vc.UserId == user.Id && vc.Purpose == "PasswordReset" && !vc.IsUsed && vc.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
            
            foreach (var vc in userCodes)
            {
                vc.Attempts++;
            }
            
            if (userCodes.Any())
            {
                await _dbContext.SaveChangesAsync();
            }
            
            return BadRequest("Invalid reset code");
        }

        // Reset password directly - validate password BEFORE marking code as used
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            return BadRequest(removeResult.Errors.Select(e => e.Description).ToArray());
        }

        var addResult = await _userManager.AddPasswordAsync(user, dto.NewPassword);
        
        if (addResult.Succeeded)
        {
            // Only mark code as used after successful password reset
            verificationCode.IsUsed = true;
            
            // Update security stamp to sign out all devices
            await _userManager.UpdateSecurityStampAsync(user);
            
            // Revoke all refresh tokens for security
            var devices = await _dbContext.UserDevices
                .Where(d => d.UserId == user.Id)
                .Include(d => d.RefreshTokens)
                .ToListAsync();

            foreach (var device in devices)
            {
                foreach (var tk in device.RefreshTokens.Where(t => !t.Revoked))
                {
                    tk.Revoked = true;
                }
            }

            await _dbContext.SaveChangesAsync();

            // Log account activity
            var accountLog = new AccountLog
            {
                UserId = user.Id,
                Action = "ResetPassword",
                Timestamp = DateTime.UtcNow
            };
            _dbContext.AccountLogs.Add(accountLog);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Password reset" });
        }

        // Password validation failed - return errors without marking code as used
        return BadRequest(addResult.Errors.Select(e => e.Description).ToArray());
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // ASP.NET Core maps JWT claims to XML schema claim types
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                     User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (result.Succeeded)
        {
            // Get current device identifier
            var currentDeviceIdentifier = GetOrCreateDeviceId();
            
            // Revoke refresh tokens from all OTHER devices (keep current device active)
            var devices = await _dbContext.UserDevices
                .Where(d => d.UserId == user.Id)
                .Include(d => d.RefreshTokens)
                .ToListAsync();

            foreach (var device in devices)
            {
                // Skip the current device - keep it active
                if (device.DeviceIdentifier == currentDeviceIdentifier)
                {
                    continue;
                }
                
                // Revoke all refresh tokens for other devices
                foreach (var token in device.RefreshTokens.Where(t => !t.Revoked))
                {
                    token.Revoked = true;
                }
            }

            await _dbContext.SaveChangesAsync();

            // Log account activity
            var accountLog = new AccountLog
            {
                UserId = user.Id,
                Action = "ChangePassword",
                Timestamp = DateTime.UtcNow,
                AdditionalContext = "Password changed successfully"
            };
            _dbContext.AccountLogs.Add(accountLog);
            await _dbContext.SaveChangesAsync();

            // Send email confirmation and save notification
            try
            {
                await _emailService.SendPasswordChangedConfirmationAsync(user.Email!, user.UserName!);
                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    "Password Changed",
                    "Your password has been successfully changed.",
                    "PasswordChanged"
                );
            }
            catch
            {
                // Log but don't fail the password change
            }

            return Ok(new { Message = "Password changed" });
        }

        return BadRequest(result.Errors.Select(e => e.Description).ToArray());
    }

    [Authorize]
    [HttpDelete("device/{deviceId}")]
    public async Task<IActionResult> RevokeDeviceAccess(int deviceId)
    {
        // ASP.NET Core maps JWT claims to XML schema claim types
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                     User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var device = await _dbContext.UserDevices
            .Include(d => d.RefreshTokens)
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId);

        if (device == null)
        {
            return NotFound("Device not found");
        }

        // Revoke all refresh tokens for this device
        foreach (var token in device.RefreshTokens.Where(t => !t.Revoked))
        {
            token.Revoked = true;
        }

        // Optionally delete the device (or just mark tokens as revoked)
        // _dbContext.UserDevices.Remove(device);

        await _dbContext.SaveChangesAsync();

        // Log account activity
        var accountLog = new AccountLog
        {
            UserId = userId,
            Action = "RevokeDeviceAccess",
            Timestamp = DateTime.UtcNow,
            AdditionalContext = $"Device access revoked: {device.DeviceIdentifier}"
        };
        _dbContext.AccountLogs.Add(accountLog);
        await _dbContext.SaveChangesAsync();

        return Ok(new { Message = "Device revoked" });
    }

    [Authorize]
    [HttpGet("user")]
    public async Task<IActionResult> GetCurrentUser()
    {
        // ASP.NET Core maps JWT claims to XML schema claim types
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                     User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            username = user.UserName,
            email = user.Email
        });
    }

    [Authorize]
    [HttpGet("devices")]
    public async Task<IActionResult> GetUserDevices()
    {
        // ASP.NET Core maps JWT claims to XML schema claim types
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                     User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var devices = await _dbContext.UserDevices
            .Where(d => d.UserId == userId)
            .Select(d => new
            {
                d.Id,
                d.DeviceIdentifier,
                d.IsVerified,
                d.VerifiedAt,
                d.CreatedAt,
                ActiveTokens = d.RefreshTokens.Count(t => !t.Revoked && t.ExpiresAt > DateTime.UtcNow)
            })
            .ToListAsync();

        return Ok(devices);
    }

    [Authorize]
    [HttpGet("logs")]
    public async Task<IActionResult> GetAccountLogs()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                     User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var logs = await _dbContext.AccountLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .Select(l => new
            {
                l.Id,
                l.Action,
                l.Timestamp,
                l.AdditionalContext
            })
            .ToListAsync();

        return Ok(logs);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                     User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var changes = new List<string>();

        // Update username if provided
        if (!string.IsNullOrEmpty(dto.Username) && dto.Username != user.UserName)
        {
            var existingUser = await _userManager.FindByNameAsync(dto.Username);
            if (existingUser != null && existingUser.Id != userId)
            {
                return BadRequest("Username unavailable");
            }
            user.UserName = dto.Username;
            changes.Add("Username");
        }

        // Update email if provided
        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null && existingUser.Id != userId)
            {
                return BadRequest("Email unavailable");
            }
            user.Email = dto.Email;
            user.EmailConfirmed = false; // Require email verification for new email
            changes.Add("Email");
        }

        if (changes.Count == 0)
        {
            return Ok(new { Message = "No changes made" });
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        // Log account activity
        var changeMessages = changes.Select(c => c == "Username" ? "username" : "email address");
        var accountLog = new AccountLog
        {
            UserId = userId,
            Action = "UpdateProfile",
            Timestamp = DateTime.UtcNow,
            AdditionalContext = $"Updated {string.Join(" and ", changeMessages)}"
        };
        _dbContext.AccountLogs.Add(accountLog);
        await _dbContext.SaveChangesAsync();

        // If email was changed, send verification email
        if (changes.Contains("Email"))
        {
            var emailVerificationExpiration = int.Parse(_config["App:VerificationCodeExpiration:EmailVerificationMinutes"]);
            var code = await CreateAndStoreVerificationCodeAsync(user.Id, "EmailVerification", emailVerificationExpiration);
            try
            {
                var baseUrl = _config["App:BaseUrl"];
                var verificationPath = _config["App:EmailVerificationPath"];
                var verificationUrl = $"{baseUrl}{verificationPath}";
                await _emailService.SendEmailConfirmationAsync(user.Email!, user.UserName!, code, verificationUrl);
            }
            catch
            {
                // Log but don't fail the update
            }
        }

        return Ok(new { Message = "Profile updated" });
    }

    [Authorize]
    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                     User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        // Step 1: Get all vaults owned by user and delete them
        var ownedVaults = await _dbContext.Vaults
            .Where(v => v.OwnerId == userId)
            .Select(v => v.Id)
            .ToListAsync();

        // Delete all owned vaults (this will cascade delete items, members, invites, etc.)
        foreach (var vaultId in ownedVaults)
        {
            try
            {
                await _vaultService.DeleteVaultAsync(vaultId, userId);
            }
            catch
            {
                // Continue with deletion even if some vaults fail
            }
        }

        // Step 2: Delete all VaultMember records for this user (in vaults they don't own)
        // This is necessary because VaultMember.User has Restrict constraint
        // Note: VaultItemVisibilities will be cascade deleted when VaultMembers are deleted
        // Owned vaults were already deleted in Step 1, so their VaultMembers are already gone
        var allMemberRecords = await _dbContext.VaultMembers
            .Where(m => m.UserId == userId)
            .ToListAsync();

        _dbContext.VaultMembers.RemoveRange(allMemberRecords);

        // Step 2b: Cancel invites created by the user
        // Since InviterId has Restrict, we need to handle invites
        var invitesByUser = await _dbContext.VaultInvites
            .Where(i => i.InviterId == userId && 
                       (i.Status == InviteStatus.Pending || i.Status == InviteStatus.Sent))
            .ToListAsync();

        foreach (var invite in invitesByUser)
        {
            try
            {
                // Cancel pending/sent invites (they can't be transferred)
                invite.Status = InviteStatus.Cancelled;
            }
            catch
            {
                // Continue even if some fail
            }
        }

        // Step 3: Handle items created by the user in vaults they don't own
        // Transfer ownership of items to the vault owner
        var itemsCreatedByUser = await _dbContext.VaultItems
            .Include(i => i.Vault)
            .Where(i => i.CreatedByUserId == userId && 
                       i.Vault.OwnerId != userId && 
                       i.Status != ItemStatus.Deleted)
            .ToListAsync();

        // Group items by vault to log once per vault
        var itemsByVault = itemsCreatedByUser.GroupBy(i => i.VaultId);

        foreach (var vaultGroup in itemsByVault)
        {
            var vaultId = vaultGroup.Key;
            var itemsInVault = vaultGroup.ToList();
            var vaultOwnerId = itemsInVault.First().Vault.OwnerId;

            foreach (var item in itemsInVault)
            {
                try
                {
                    // Transfer item creation ownership to vault owner
                    item.CreatedByUserId = vaultOwnerId;
                }
                catch
                {
                    // Continue even if some fail
                }
            }

            // Log vault activity for item ownership transfer
            try
            {
                var deletedUser = await _userManager.FindByIdAsync(userId);
                var deletedUserName = deletedUser?.UserName ?? deletedUser?.Email ?? "Deleted User";
                
                var vaultLog = new VaultLog
                {
                    VaultId = vaultId,
                    UserId = vaultOwnerId, // Log as vault owner since they now own the items
                    Action = "TransferItemOwnership",
                    Timestamp = DateTime.UtcNow,
                    TargetUserId = userId,
                    AdditionalContext = $"Item ownership transferred from {deletedUserName}'s deleted account: {itemsInVault.Count} item(s) now owned by vault owner"
                };
                _dbContext.VaultLogs.Add(vaultLog);
            }
            catch
            {
                // Continue even if logging fails
            }
        }

        // Step 4: Delete AccountLogs (they reference the user with Restrict)
        var accountLogs = await _dbContext.AccountLogs
            .Where(l => l.UserId == userId)
            .ToListAsync();
        _dbContext.AccountLogs.RemoveRange(accountLogs);

        // Step 5: Delete VaultLogs where user is the actor (they reference the user with Restrict)
        // Note: VaultLogs with TargetUserId will be set to null automatically (SetNull)
        var vaultLogsByUser = await _dbContext.VaultLogs
            .Where(l => l.UserId == userId)
            .ToListAsync();
        _dbContext.VaultLogs.RemoveRange(vaultLogsByUser);

        // Save all changes before deleting user
        await _dbContext.SaveChangesAsync();

        // Step 6: Send email confirmation before deleting user
        try
        {
            await _emailService.SendAccountDeletedConfirmationAsync(user.Email!, user.UserName!);
            // Note: Don't save notification for account deletion since account will be deleted
        }
        catch
        {
            // Log but don't fail the deletion
        }

        // Step 7: Delete user account
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        return Ok(new { Message = "Account deleted" });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // ASP.NET Core maps JWT claims to XML schema claim types
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                     User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var deviceIdentifier = GetOrCreateDeviceId();
        var device = await _dbContext.UserDevices
            .Include(d => d.RefreshTokens)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceIdentifier == deviceIdentifier);

        if (device != null)
        {
            // Revoke all refresh tokens for this device
            foreach (var token in device.RefreshTokens.Where(t => !t.Revoked))
            {
                token.Revoked = true;
            }

            await _dbContext.SaveChangesAsync();
        }

        // Log account activity
        var accountLog = new AccountLog
        {
            UserId = userId,
            Action = "Logout",
            Timestamp = DateTime.UtcNow,
            AdditionalContext = $"Logged out from device: {deviceIdentifier}"
        };
        _dbContext.AccountLogs.Add(accountLog);
        await _dbContext.SaveChangesAsync();

        // Clear refresh token cookie
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // HTTPS only (recommended for production)
            SameSite = SameSiteMode.Lax, // Must match the cookie settings used when setting it
            Path = "/"
        });

        return Ok(new { Message = "Logged out" });
    }

    // --------------------
    // Helper functions below
    // --------------------

    // Helper function to save refresh token on http cookies
    private void SetRefreshCookie(RefreshToken token, bool isPersistent = true)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // HTTPS only (recommended for production)
            SameSite = SameSiteMode.Lax, // Works for same-site requests (www.eloomen.com <-> api.eloomen.com)
            Path = "/"
        };
        
        // Only set Expires for persistent cookies (Remember Me checked)
        // Session cookies (Remember Me unchecked) will be deleted when browser closes
        if (isPersistent)
        {
            cookieOptions.Expires = token.ExpiresAt;
        }
        
        Response.Cookies.Append("refreshToken", token.Token, cookieOptions);
    }
    
    // Helper methods for verification codes
    private string GenerateVerificationCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }
    
    private string HashCode(string code)
    {
        return Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(code))
        );
    }
    
    private async Task<string> CreateAndStoreVerificationCodeAsync(string userId, string purpose, int expirationMinutes)
    {
        // Invalidate any existing codes for this user and purpose
        var existingCodes = await _dbContext.VerificationCodes
            .Where(vc => vc.UserId == userId && vc.Purpose == purpose && !vc.IsUsed && vc.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        
        foreach (var cd in existingCodes)
        {
            cd.IsUsed = true;
        }
        
        // Generate new code
        var code = GenerateVerificationCode();
        var codeHash = HashCode(code);
        
        var verificationCode = new VerificationCode
        {
            UserId = userId,
            CodeHash = codeHash,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            IsUsed = false,
            Attempts = 0,
        };
        
        _dbContext.VerificationCodes.Add(verificationCode);
        await _dbContext.SaveChangesAsync();
        
        return code;
    }
    
    private async Task<VerificationCode?> VerifyCodeAsync(string userId, string code, string purpose)
    {
        var codeHash = HashCode(code);
        
        var verificationCode = await _dbContext.VerificationCodes
            .FirstOrDefaultAsync(vc => 
                vc.UserId == userId && 
                vc.CodeHash == codeHash && 
                vc.Purpose == purpose &&
                !vc.IsUsed &&
                vc.ExpiresAt > DateTime.UtcNow &&
                vc.Attempts <= 5);
        
        if (verificationCode == null)
        {
            // Increment attempts for failed verification (optional: track for security)
            var userCodes = await _dbContext.VerificationCodes
                .Where(vc => vc.UserId == userId && vc.Purpose == purpose && !vc.IsUsed && vc.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
            
            foreach (var vc in userCodes)
            {
                vc.Attempts++;
            }
            
            if (userCodes.Any())
            {
                await _dbContext.SaveChangesAsync();
            }
            
            return null;
        }
        
        // Mark as used
        verificationCode.IsUsed = true;
        await _dbContext.SaveChangesAsync();
        
        return verificationCode;
    }

    // Helper function to get or generate device ID
    // Best practice: Backend generates device ID and stores in secure cookie
    // Falls back to header if cookie missing (backward compatibility)
    // Returns generated device ID and sets cookie if needed
    private string GetOrCreateDeviceId()
    {
        // First, check for device ID in secure cookie (most reliable)
        var deviceIdFromCookie = Request.Cookies["deviceId"];
        if (!string.IsNullOrWhiteSpace(deviceIdFromCookie))
        {
            return deviceIdFromCookie;
        }
        
        // No valid device ID found, generate a new GUID (backend-controlled)
        var newDeviceId = Guid.NewGuid().ToString();
        SetDeviceIdCookie(newDeviceId);
        return newDeviceId;
    }
    
    // Helper function to set device ID cookie
    private void SetDeviceIdCookie(string deviceId)
    {
        // Use Lax for same-site requests (www.eloomen.com <-> api.eloomen.com share the same root domain)
        // Lax is more secure than None and works fine for subdomains of the same root domain
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // Prevents JavaScript access (security)
            Secure = true, // HTTPS only (recommended for production)
            SameSite = SameSiteMode.Lax, // Works for same-site requests (subdomains of eloomen.com)
            Path = "/", // Available across all routes
            Expires = DateTimeOffset.UtcNow.AddYears(1) // Long-lived cookie (1 year)
        };
        
        Response.Cookies.Append("deviceId", deviceId, cookieOptions);
    }
}
