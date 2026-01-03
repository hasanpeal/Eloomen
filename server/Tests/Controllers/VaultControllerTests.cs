using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using server.Controllers;
using server.Dtos.Vault;
using server.Interfaces;
using server.Models;
using System.Security.Claims;

namespace server.Tests.Controllers;

public class VaultControllerTests
{
    private readonly Mock<IVaultService> _vaultServiceMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly VaultController _controller;
    private readonly string _testUserId = Guid.NewGuid().ToString();

    public VaultControllerTests()
    {
        _vaultServiceMock = new Mock<IVaultService>();
        _userManagerMock = CreateUserManagerMock();
        _controller = new VaultController(_vaultServiceMock.Object, _userManagerMock.Object);
        
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
    public async Task GetUserVaults_ReturnsVaults()
    {
        // Arrange
        var vaults = new List<VaultResponseDTO>
        {
            new VaultResponseDTO { Id = 1, Name = "Vault 1", OwnerId = _testUserId }
        };
        _vaultServiceMock.Setup(x => x.GetUserVaultsAsync(_testUserId))
            .ReturnsAsync(vaults);

        // Act
        var result = await _controller.GetUserVaults();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedVaults = Assert.IsType<List<VaultResponseDTO>>(okResult.Value);
        Assert.Single(returnedVaults);
    }

    [Fact]
    public async Task GetVault_WithValidId_ReturnsVault()
    {
        // Arrange
        var vault = new VaultResponseDTO { Id = 1, Name = "Test Vault", OwnerId = _testUserId };
        _vaultServiceMock.Setup(x => x.GetVaultByIdAsync(1, _testUserId))
            .ReturnsAsync(vault);

        // Act
        var result = await _controller.GetVault(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedVault = Assert.IsType<VaultResponseDTO>(okResult.Value);
        Assert.Equal(1, returnedVault.Id);
    }

    [Fact]
    public async Task GetVault_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.GetVaultByIdAsync(999, _testUserId))
            .ReturnsAsync((VaultResponseDTO?)null);

        // Act
        var result = await _controller.GetVault(999);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Vault not found or access denied", notFound.Value);
    }

    [Fact]
    public async Task CreateVault_WithValidData_CreatesVault()
    {
        // Arrange
        var dto = new CreateVaultDTO
        {
            Name = "New Vault",
            Description = "Description",
            PolicyType = PolicyType.ManualRelease
        };
        var vault = new VaultResponseDTO { Id = 1, Name = dto.Name, OwnerId = _testUserId };
        _vaultServiceMock.Setup(x => x.CreateVaultAsync(dto, _testUserId))
            .ReturnsAsync(vault);

        // Act
        var result = await _controller.CreateVault(dto);

        // Assert
        var createdAt = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedVault = Assert.IsType<VaultResponseDTO>(createdAt.Value);
        Assert.Equal(1, returnedVault.Id);
    }

    [Fact]
    public async Task UpdateVault_WithValidData_UpdatesVault()
    {
        // Arrange
        var dto = new UpdateVaultDTO { Name = "Updated Name" };
        var vault = new VaultResponseDTO { Id = 1, Name = dto.Name, OwnerId = _testUserId };
        _vaultServiceMock.Setup(x => x.UpdateVaultAsync(1, dto, _testUserId))
            .ReturnsAsync(vault);

        // Act
        var result = await _controller.UpdateVault(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedVault = Assert.IsType<VaultResponseDTO>(okResult.Value);
        Assert.Equal("Updated Name", returnedVault.Name);
    }

    [Fact]
    public async Task DeleteVault_WithValidId_DeletesVault()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.DeleteVaultAsync(1, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteVault(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task CreateInvite_WithValidData_CreatesInvite()
    {
        // Arrange
        var dto = new CreateInviteDTO
        {
            InviteeEmail = "invitee@example.com",
            Privilege = Privilege.Member
        };
        var invite = new VaultInviteResponseDTO
        {
            Id = 1,
            InviteeEmail = dto.InviteeEmail,
            Privilege = dto.Privilege
        };
        _vaultServiceMock.Setup(x => x.CreateInviteAsync(1, dto, _testUserId))
            .ReturnsAsync(invite);

        // Act
        var result = await _controller.CreateInvite(1, dto);

        // Assert
        var createdAt = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedInvite = Assert.IsType<VaultInviteResponseDTO>(createdAt.Value);
        Assert.Equal(1, returnedInvite.Id);
    }

    [Fact]
    public async Task GetVaultInvites_ReturnsInvites()
    {
        // Arrange
        var invites = new List<VaultInviteResponseDTO>
        {
            new VaultInviteResponseDTO { Id = 1, InviteeEmail = "test@example.com" }
        };
        _vaultServiceMock.Setup(x => x.GetVaultInvitesAsync(1, _testUserId))
            .ReturnsAsync(invites);

        // Act
        var result = await _controller.GetVaultInvites(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInvites = Assert.IsType<List<VaultInviteResponseDTO>>(okResult.Value);
        Assert.Single(returnedInvites);
    }

    [Fact]
    public async Task CancelInvite_WithValidId_CancelsInvite()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.CancelInviteAsync(1, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelInvite(1, 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetVaultMembers_ReturnsMembers()
    {
        // Arrange
        var members = new List<VaultMemberResponseDTO>
        {
            new VaultMemberResponseDTO { Id = 1, UserId = _testUserId, Privilege = Privilege.Owner }
        };
        _vaultServiceMock.Setup(x => x.GetVaultMembersAsync(1, _testUserId))
            .ReturnsAsync(members);

        // Act
        var result = await _controller.GetVaultMembers(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMembers = Assert.IsType<List<VaultMemberResponseDTO>>(okResult.Value);
        Assert.Single(returnedMembers);
    }

    [Fact]
    public async Task RemoveMember_WithValidId_RemovesMember()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.RemoveMemberAsync(1, 1, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RemoveMember(1, 1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateMemberPrivilege_WithValidData_UpdatesPrivilege()
    {
        // Arrange
        var dto = new UpdateMemberPrivilegeDTO
        {
            MemberId = 1,
            Privilege = Privilege.Admin
        };
        _vaultServiceMock.Setup(x => x.UpdateMemberPrivilegeAsync(1, dto, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateMemberPrivilege(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task TransferOwnership_WithValidData_TransfersOwnership()
    {
        // Arrange
        var dto = new TransferOwnershipDTO
        {
            MemberId = 2
        };
        _vaultServiceMock.Setup(x => x.TransferOwnershipAsync(1, dto, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.TransferOwnership(1, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task LeaveVault_LeavesVault()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.LeaveVaultAsync(1, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.LeaveVault(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ReleaseVaultManually_ReleasesVault()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.ReleaseVaultManuallyAsync(1, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ReleaseVaultManually(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetVaultLogs_ReturnsLogs()
    {
        // Arrange
        var logs = new List<VaultLogResponseDTO>
        {
            new VaultLogResponseDTO { Id = 1, Action = "CreateVault", VaultId = 1 }
        };
        _vaultServiceMock.Setup(x => x.GetVaultLogsAsync(1, _testUserId))
            .ReturnsAsync(logs);

        // Act
        var result = await _controller.GetVaultLogs(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedLogs = Assert.IsType<List<VaultLogResponseDTO>>(okResult.Value);
        Assert.Single(returnedLogs);
    }
}

