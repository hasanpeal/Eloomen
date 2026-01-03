using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using server.Controllers;
using server.Dtos.Contact;
using server.Interfaces;
using server.Models;
using server.Tests.Helpers;
using System.Security.Claims;

namespace server.Tests.Controllers;

public class ContactControllerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly ContactController _controller;
    private readonly User _testUser;

    public ContactControllerTests()
    {
        _userManagerMock = CreateUserManagerMock();
        _emailServiceMock = new Mock<IEmailService>();
        _controller = new ContactController(_userManagerMock.Object, _emailServiceMock.Object);
        
        _testUser = TestHelpers.CreateTestUser();
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
    public async Task SendContact_WithValidData_SendsEmail()
    {
        // Arrange
        var dto = new ContactRequestDTO
        {
            Name = "Test User",
            Message = "Test message"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(_testUser.Id))
            .ReturnsAsync(_testUser);
        _emailServiceMock.Setup(x => x.SendContactEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SendContact(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _emailServiceMock.Verify(x => x.SendContactEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            dto.Name, dto.Message), Times.Once);
    }

    [Fact]
    public async Task SendPublicContact_WithValidData_SendsEmail()
    {
        // Arrange
        var dto = new PublicContactRequestDTO
        {
            Name = "Public User",
            Email = "public@example.com",
            Message = "Public message"
        };

        _emailServiceMock.Setup(x => x.SendPublicContactEmailAsync(
            dto.Name, dto.Email, dto.Message))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SendPublicContact(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _emailServiceMock.Verify(x => x.SendPublicContactEmailAsync(
            dto.Name, dto.Email, dto.Message), Times.Once);
    }
}

