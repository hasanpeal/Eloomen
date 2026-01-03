using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using server.Dtos.Vault;
using server.Interfaces;
using server.Models;
using server.Services;
using server.Tests.Helpers;

namespace server.Tests.Services;

public class VaultServiceTests : IDisposable
{
    private readonly ApplicationDBContext _dbContext;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IS3Service> _s3ServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly VaultService _vaultService;
    private readonly User _testUser;

    public VaultServiceTests()
    {
        _dbContext = TestHelpers.CreateInMemoryDbContext();
        _userManagerMock = CreateUserManagerMock();
        _emailServiceMock = new Mock<IEmailService>();
        _configMock = new Mock<IConfiguration>();
        _s3ServiceMock = new Mock<IS3Service>();
        _notificationServiceMock = new Mock<INotificationService>();

        _testUser = TestHelpers.CreateTestUser();
        _dbContext.Users.Add(_testUser);
        _dbContext.SaveChanges();

        _vaultService = new VaultService(
            _dbContext,
            _userManagerMock.Object,
            _emailServiceMock.Object,
            _configMock.Object,
            _s3ServiceMock.Object,
            _notificationServiceMock.Object
        );
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

    [Fact]
    public async Task CreateVaultAsync_WithValidData_ReturnsVaultResponse()
    {
        // Arrange
        var dto = new CreateVaultDTO
        {
            Name = "My Test Vault",
            Description = "Test Description",
            PolicyType = PolicyType.ManualRelease
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(_testUser.Id))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _vaultService.CreateVaultAsync(dto, _testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Description, result.Description);
        Assert.Equal(_testUser.Id, result.OwnerId);
        Assert.Equal(Privilege.Owner, result.UserPrivilege);
        Assert.Equal(VaultStatus.Active, result.Status);

        // Verify vault was saved to database
        var vaultInDb = await _dbContext.Vaults.FirstOrDefaultAsync(v => v.Id == result.Id);
        Assert.NotNull(vaultInDb);
        Assert.Equal(dto.Name, vaultInDb.Name);
    }

    [Fact]
    public async Task GetVaultByIdAsync_AsOwner_ReturnsVault()
    {
        // Arrange
        var vault = TestHelpers.CreateTestVault(_testUser.Id);
        _dbContext.Vaults.Add(vault);
        await _dbContext.SaveChangesAsync();

        _userManagerMock.Setup(x => x.FindByIdAsync(_testUser.Id))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _vaultService.GetVaultByIdAsync(vault.Id, _testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(vault.Id, result.Id);
        Assert.Equal(Privilege.Owner, result.UserPrivilege);
    }

    [Fact]
    public async Task GetVaultByIdAsync_AsNonMember_ReturnsNull()
    {
        // Arrange
        var otherUser = TestHelpers.CreateTestUser(email: "other@example.com", username: "otheruser");
        _dbContext.Users.Add(otherUser);

        var vault = TestHelpers.CreateTestVault(_testUser.Id);
        _dbContext.Vaults.Add(vault);
        await _dbContext.SaveChangesAsync();

        _userManagerMock.Setup(x => x.FindByIdAsync(_testUser.Id))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _vaultService.GetVaultByIdAsync(vault.Id, otherUser.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetVaultByIdAsync_AsMember_ReturnsVault()
    {
        // Arrange
        var memberUser = TestHelpers.CreateTestUser(email: "member@example.com", username: "member");
        _dbContext.Users.Add(memberUser);

        var vault = TestHelpers.CreateTestVault(_testUser.Id);
        _dbContext.Vaults.Add(vault);

        var member = new VaultMember
        {
            VaultId = vault.Id,
            UserId = memberUser.Id,
            Privilege = Privilege.Member,
            Status = MemberStatus.Active,
            AddedById = _testUser.Id
        };
        _dbContext.VaultMembers.Add(member);
        await _dbContext.SaveChangesAsync();

        _userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => id == _testUser.Id ? _testUser : memberUser);

        // Act
        var result = await _vaultService.GetVaultByIdAsync(vault.Id, memberUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(vault.Id, result.Id);
        Assert.Equal(Privilege.Member, result.UserPrivilege);
    }

    [Fact]
    public async Task GetUserVaultsAsync_ReturnsOnlyUserVaults()
    {
        // Arrange
        var otherUser = TestHelpers.CreateTestUser(email: "other@example.com", username: "otheruser");
        _dbContext.Users.Add(otherUser);

        var vault1 = TestHelpers.CreateTestVault(_testUser.Id, "Vault 1");
        var vault2 = TestHelpers.CreateTestVault(_testUser.Id, "Vault 2");
        var vault3 = TestHelpers.CreateTestVault(otherUser.Id, "Other Vault");
        
        _dbContext.Vaults.AddRange(vault1, vault2, vault3);
        await _dbContext.SaveChangesAsync();

        _userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => id == _testUser.Id ? _testUser : otherUser);

        // Act
        var result = await _vaultService.GetUserVaultsAsync(_testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, v => Assert.True(v.OwnerId == _testUser.Id || v.UserPrivilege != null));
    }

    [Fact]
    public async Task UpdateVaultAsync_AsOwner_UpdatesVault()
    {
        // Arrange
        var vault = TestHelpers.CreateTestVault(_testUser.Id, "Original Name");
        _dbContext.Vaults.Add(vault);
        await _dbContext.SaveChangesAsync();

        var updateDto = new UpdateVaultDTO
        {
            Name = "Updated Name",
            Description = "Updated Description"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(_testUser.Id))
            .ReturnsAsync(_testUser);

        // Act
        var result = await _vaultService.UpdateVaultAsync(vault.Id, updateDto, _testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateDto.Name, result.Name);
        Assert.Equal(updateDto.Description, result.Description);

        var vaultInDb = await _dbContext.Vaults.FindAsync(vault.Id);
        Assert.NotNull(vaultInDb);
        Assert.Equal(updateDto.Name, vaultInDb.Name);
    }

    [Fact]
    public async Task UpdateVaultAsync_AsNonOwner_ReturnsNull()
    {
        // Arrange
        var otherUser = TestHelpers.CreateTestUser(email: "other@example.com", username: "otheruser");
        _dbContext.Users.Add(otherUser);

        var vault = TestHelpers.CreateTestVault(_testUser.Id);
        _dbContext.Vaults.Add(vault);
        await _dbContext.SaveChangesAsync();

        var updateDto = new UpdateVaultDTO
        {
            Name = "Updated Name"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(otherUser.Id))
            .ReturnsAsync(otherUser);

        // Act
        var result = await _vaultService.UpdateVaultAsync(vault.Id, updateDto, otherUser.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteVaultAsync_AsOwner_DeletesVault()
    {
        // Arrange
        var vault = TestHelpers.CreateTestVault(_testUser.Id);
        _dbContext.Vaults.Add(vault);
        await _dbContext.SaveChangesAsync();

        _userManagerMock.Setup(x => x.FindByIdAsync(_testUser.Id))
            .ReturnsAsync(_testUser);
        
        // Mock email and notification services to prevent actual calls
        _emailServiceMock.Setup(x => x.SendVaultDeletedNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(x => x.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _vaultService.DeleteVaultAsync(vault.Id, _testUser.Id);

        // Assert
        Assert.True(result);

        // The service creates a VaultLog before deleting, which causes a foreign key constraint.
        // Delete any logs that reference this vault first, then check vault is deleted
        var logs = await _dbContext.VaultLogs.Where(l => l.VaultId == vault.Id).ToListAsync();
        if (logs.Any())
        {
            _dbContext.VaultLogs.RemoveRange(logs);
            await _dbContext.SaveChangesAsync();
        }

        // Now the vault should be removed from database (hard delete)
        var vaultInDb = await _dbContext.Vaults.FindAsync(vault.Id);
        Assert.Null(vaultInDb);
    }

    [Fact]
    public async Task DeleteVaultAsync_AsNonOwner_ReturnsFalse()
    {
        // Arrange
        var otherUser = TestHelpers.CreateTestUser(email: "other@example.com", username: "otheruser");
        _dbContext.Users.Add(otherUser);

        var vault = TestHelpers.CreateTestVault(_testUser.Id);
        _dbContext.Vaults.Add(vault);
        await _dbContext.SaveChangesAsync();

        _userManagerMock.Setup(x => x.FindByIdAsync(otherUser.Id))
            .ReturnsAsync(otherUser);

        // Act
        var result = await _vaultService.DeleteVaultAsync(vault.Id, otherUser.Id);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

