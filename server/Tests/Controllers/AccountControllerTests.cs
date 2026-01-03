using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using server.Controllers;
using server.Dtos.Account;
using server.Interfaces;
using server.Models;
using server.Services;
using server.Tests.Helpers;
using System.Security.Claims;

namespace server.Tests.Controllers;

public class AccountControllerTests : IDisposable
{
    private readonly ApplicationDBContext _dbContext;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IVaultService> _vaultServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly AccountController _controller;
    private readonly User _testUser;

    public AccountControllerTests()
    {
        _dbContext = TestHelpers.CreateInMemoryDbContext();
        _userManagerMock = CreateUserManagerMock();
        _signInManagerMock = CreateSignInManagerMock();
        _tokenServiceMock = new Mock<ITokenService>();
        _deviceServiceMock = new Mock<IDeviceService>();
        _emailServiceMock = new Mock<IEmailService>();
        _configMock = new Mock<IConfiguration>();
        _vaultServiceMock = new Mock<IVaultService>();
        _notificationServiceMock = new Mock<INotificationService>();

        _testUser = TestHelpers.CreateTestUser();
        _dbContext.Users.Add(_testUser);
        _dbContext.SaveChanges();

        SetupConfiguration();

        _controller = new AccountController(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _tokenServiceMock.Object,
            _deviceServiceMock.Object,
            _emailServiceMock.Object,
            _configMock.Object,
            _dbContext,
            _vaultServiceMock.Object,
            _notificationServiceMock.Object
        );

        SetupControllerContext();
    }

    private Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>();
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<UserManager<User>>>();
        
        return new Mock<UserManager<User>>(
            store.Object, options, new PasswordHasher<User>(), userValidators, passwordValidators, 
            keyNormalizer, errors, services.Object, logger.Object);
    }

    private Mock<SignInManager<User>> CreateSignInManagerMock()
    {
        var userManager = _userManagerMock.Object;
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<SignInManager<User>>>();
        
        return new Mock<SignInManager<User>>(
            userManager, contextAccessor.Object, claimsFactory.Object, options, logger.Object, 
            new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>().Object,
            new Mock<Microsoft.AspNetCore.Identity.IUserConfirmation<User>>().Object);
    }

    private void SetupConfiguration()
    {
        _configMock.Setup(x => x["App:BaseUrl"]).Returns("http://localhost:3000");
        _configMock.Setup(x => x["App:EmailVerificationPath"]).Returns("/verify-email");
        _configMock.Setup(x => x["App:DeviceVerificationPath"]).Returns("/verify-device");
        _configMock.Setup(x => x["App:PasswordResetPath"]).Returns("/reset-password");
        _configMock.Setup(x => x["App:VerificationCodeExpiration:EmailVerificationMinutes"]).Returns("30");
        _configMock.Setup(x => x["App:VerificationCodeExpiration:DeviceVerificationMinutes"]).Returns("15");
        _configMock.Setup(x => x["App:VerificationCodeExpiration:PasswordResetMinutes"]).Returns("60");
        _configMock.Setup(x => x["Jwt:RefreshTokenDays"]).Returns("30");
    }

    private void SetupControllerContext()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUser.Id),
            new Claim("sub", _testUser.Id)
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
    public async Task Register_WithValidData_CreatesUser()
    {
        // Arrange
        var dto = new RegisterDTO
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _userManagerMock.Setup(x => x.FindByNameAsync(dto.Username)).ReturnsAsync((User?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "User"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => new User { Id = id, Email = dto.Email, UserName = dto.Username });

        var device = new UserDevice { UserId = _testUser.Id, DeviceIdentifier = "test-device", IsVerified = false };
        _deviceServiceMock.Setup(x => x.GetOrCreateDeviceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(device);
        _emailServiceMock.Setup(x => x.SendEmailConfirmationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var dto = new RegisterDTO
        {
            Username = "newuser",
            Email = _testUser.Email!,
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(_testUser);

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Account already exists", badRequest.Value);
    }

    [Fact]
    public async Task Register_WithUsernameMatchingExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        // Ensure test user is in database with a known email
        var testUserEmail = "existing@example.com";
        var testUser = TestHelpers.CreateTestUser(email: testUserEmail, username: "existinguser");
        _dbContext.Users.Add(testUser);
        await _dbContext.SaveChangesAsync();

        var dto = new RegisterDTO
        {
            Username = testUserEmail, // Username matches existing email
            Email = "newemail@example.com",
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _userManagerMock.Setup(x => x.FindByNameAsync(dto.Username)).ReturnsAsync((User?)null);

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Username taken", badRequest.Value);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var dto = new LoginDTO
        {
            UsernameOrEmail = _testUser.UserName!,
            Password = "Password123!",
            RememberMe = false
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(dto.UsernameOrEmail)).ReturnsAsync(_testUser);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(_testUser, dto.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var device = new UserDevice 
        { 
            UserId = _testUser.Id, 
            DeviceIdentifier = "test-device", 
            IsVerified = true 
        };
        _deviceServiceMock.Setup(x => x.GetOrCreateDeviceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(device);

        _tokenServiceMock.Setup(x => x.CreateRefreshToken()).Returns("refresh-token");
        _tokenServiceMock.Setup(x => x.CreateToken(_testUser)).Returns("access-token");

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new LoginDTO
        {
            UsernameOrEmail = "nonexistent",
            Password = "WrongPassword"
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(dto.UsernameOrEmail)).ReturnsAsync((User?)null);
        _userManagerMock.Setup(x => x.FindByEmailAsync(dto.UsernameOrEmail)).ReturnsAsync((User?)null);

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Login credentials are incorrect", unauthorized.Value);
    }

    [Fact]
    public async Task VerifyEmail_WithValidCode_VerifiesEmail()
    {
        // Arrange
        var dto = new VerifyEmailDTO
        {
            Email = _testUser.Email!,
            Code = "123456"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(_testUser);
        _userManagerMock.Setup(x => x.UpdateAsync(_testUser)).ReturnsAsync(IdentityResult.Success);

        var device = new UserDevice { UserId = _testUser.Id, DeviceIdentifier = "test-device", IsVerified = false };
        _deviceServiceMock.Setup(x => x.GetOrCreateDeviceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(device);

        // Create a valid verification code
        var codeHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(dto.Code)));
        var verificationCode = new VerificationCode
        {
            UserId = _testUser.Id,
            CodeHash = codeHash,
            Purpose = "EmailVerification",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsUsed = false,
            Attempts = 0
        };
        _dbContext.VerificationCodes.Add(verificationCode);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.VerifyEmail(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidCode_ReturnsBadRequest()
    {
        // Arrange
        var dto = new VerifyEmailDTO
        {
            Email = _testUser.Email!,
            Code = "000000"
        };

        _testUser.EmailConfirmed = false;
        _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(_testUser);

        // Act
        var result = await _controller.VerifyEmail(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid verification code", badRequest.Value);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewToken()
    {
        // Arrange
        var refreshTokenValue = "valid-refresh-token";
        var device = new UserDevice
        {
            Id = 1,
            UserId = _testUser.Id,
            DeviceIdentifier = "test-device",
            IsVerified = true,
            User = _testUser
        };
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserDeviceId = device.Id,
            UserDevice = device,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            Revoked = false
        };
        _dbContext.UserDevices.Add(device);
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        var cookieCollection = new MockCookieCollection();
        cookieCollection.Add("refreshToken", refreshTokenValue);
        _controller.ControllerContext.HttpContext.Request.Cookies = cookieCollection;

        _tokenServiceMock.Setup(x => x.CreateRefreshToken()).Returns("new-refresh-token");
        _tokenServiceMock.Setup(x => x.CreateToken(_testUser)).Returns("new-access-token");

        // Act
        var result = await _controller.Refresh();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsUserInfo()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByIdAsync(_testUser.Id))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetUserDevices_ReturnsDevices()
    {
        // Arrange
        var device = new UserDevice
        {
            UserId = _testUser.Id,
            DeviceIdentifier = "test-device",
            IsVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.UserDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetUserDevices();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAccountLogs_ReturnsLogs()
    {
        // Arrange
        var log = new AccountLog
        {
            UserId = _testUser.Id,
            Action = "TestAction",
            Timestamp = DateTime.UtcNow
        };
        _dbContext.AccountLogs.Add(log);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAccountLogs();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_UpdatesProfile()
    {
        // Arrange
        var dto = new UpdateProfileDTO
        {
            Username = "newusername"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(_testUser.Id)).ReturnsAsync(_testUser);
        _userManagerMock.Setup(x => x.FindByNameAsync(dto.Username)).ReturnsAsync((User?)null);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.UpdateProfile(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WithValidPassword_ChangesPassword()
    {
        // Arrange
        var dto = new ChangePasswordDTO
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(_testUser.Id)).ReturnsAsync(_testUser);
        _userManagerMock.Setup(x => x.ChangePasswordAsync(_testUser, dto.CurrentPassword, dto.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        _emailServiceMock.Setup(x => x.SendPasswordChangedConfirmationAsync(
            It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(x => x.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ChangePassword(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ResendVerification_WithValidEmail_SendsVerification()
    {
        // Arrange
        var dto = new ForgotPasswordDTO { Email = _testUser.Email! };
        _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(_testUser);
        _emailServiceMock.Setup(x => x.SendEmailConfirmationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ResendVerification(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task VerifyDevice_WithValidCode_VerifiesDevice()
    {
        // Arrange
        var dto = new VerifyDeviceDTO
        {
            UsernameOrEmail = _testUser.UserName!,
            Code = "123456"
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(dto.UsernameOrEmail)).ReturnsAsync(_testUser);
        
        var device = new UserDevice
        {
            UserId = _testUser.Id,
            DeviceIdentifier = "test-device",
            IsVerified = false
        };
        _dbContext.UserDevices.Add(device);
        
        var codeHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(dto.Code)));
        var verificationCode = new VerificationCode
        {
            UserId = _testUser.Id,
            CodeHash = codeHash,
            Purpose = "DeviceVerification",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false,
            Attempts = 0
        };
        _dbContext.VerificationCodes.Add(verificationCode);
        await _dbContext.SaveChangesAsync();

        _tokenServiceMock.Setup(x => x.CreateRefreshToken()).Returns("refresh-token");
        _tokenServiceMock.Setup(x => x.CreateToken(_testUser)).Returns("access-token");

        var cookieCollection = new MockCookieCollection();
        cookieCollection.Add("deviceId", "test-device");
        _controller.ControllerContext.HttpContext.Request.Cookies = cookieCollection;

        // Act
        var result = await _controller.VerifyDevice(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ForgotPassword_WithValidEmail_SendsResetCode()
    {
        // Arrange
        var dto = new ForgotPasswordDTO { Email = _testUser.Email! };
        _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(_testUser);
        _emailServiceMock.Setup(x => x.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ForgotPassword(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_WithValidCode_ResetsPassword()
    {
        // Arrange
        var dto = new ResetPasswordDTO
        {
            Email = _testUser.Email!,
            Code = "123456",
            NewPassword = "NewPassword123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(_testUser);
        _userManagerMock.Setup(x => x.RemovePasswordAsync(_testUser))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddPasswordAsync(_testUser, dto.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        var codeHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(dto.Code)));
        var verificationCode = new VerificationCode
        {
            UserId = _testUser.Id,
            CodeHash = codeHash,
            Purpose = "PasswordReset",
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            IsUsed = false,
            Attempts = 0
        };
        _dbContext.VerificationCodes.Add(verificationCode);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.ResetPassword(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Logout_RevokesTokens()
    {
        // Arrange
        var device = new UserDevice
        {
            UserId = _testUser.Id,
            DeviceIdentifier = "test-device",
            RefreshTokens = new List<RefreshToken>
            {
                new RefreshToken { Token = "token1", Revoked = false }
            }
        };
        _dbContext.UserDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        var cookieCollection = new MockCookieCollection();
        cookieCollection.Add("deviceId", "test-device");
        _controller.ControllerContext.HttpContext.Request.Cookies = cookieCollection;

        // Act
        var result = await _controller.Logout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }


    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

// Helper class for mocking cookies
public class MockCookieCollection : IRequestCookieCollection
{
    private readonly Dictionary<string, string> _cookies = new();

    public string? this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;

    public int Count => _cookies.Count;

    public ICollection<string> Keys => _cookies.Keys;

    public bool ContainsKey(string key) => _cookies.ContainsKey(key);

    public bool TryGetValue(string key, out string? value)
    {
        if (_cookies.TryGetValue(key, out var val))
        {
            value = val;
            return true;
        }
        value = null;
        return false;
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(string key, string value) => _cookies[key] = value;
}

