using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using server.Dtos.VaultItem;
using server.Interfaces;
using server.Models;
using server.Services;
using server.Tests.Helpers;

namespace server.Tests.Services;

public class VaultItemServiceExtendedTests : IDisposable
{
    private readonly ApplicationDBContext _dbContext;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IVaultService> _vaultServiceMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly Mock<IS3Service> _s3ServiceMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly VaultItemService _vaultItemService;
    private readonly User _testUser;
    private readonly Vault _testVault;

    public VaultItemServiceExtendedTests()
    {
        _dbContext = TestHelpers.CreateInMemoryDbContext();
        _userManagerMock = CreateUserManagerMock();
        _vaultServiceMock = new Mock<IVaultService>();
        _encryptionServiceMock = new Mock<IEncryptionService>();
        _s3ServiceMock = new Mock<IS3Service>();
        _configMock = new Mock<IConfiguration>();
        _emailServiceMock = new Mock<IEmailService>();
        _notificationServiceMock = new Mock<INotificationService>();

        _testUser = TestHelpers.CreateTestUser();
        _dbContext.Users.Add(_testUser);
        
        _testVault = TestHelpers.CreateTestVault(_testUser.Id);
        _dbContext.Vaults.Add(_testVault);
        
        var member = new VaultMember
        {
            VaultId = _testVault.Id,
            UserId = _testUser.Id,
            Privilege = Privilege.Owner,
            Status = MemberStatus.Active
        };
        _dbContext.VaultMembers.Add(member);
        _dbContext.SaveChanges();

        _configMock.Setup(x => x["Jwt:SigningKey"]).Returns("test-key");

        _vaultItemService = new VaultItemService(
            _dbContext,
            _userManagerMock.Object,
            _vaultServiceMock.Object,
            _encryptionServiceMock.Object,
            _s3ServiceMock.Object,
            _configMock.Object,
            _emailServiceMock.Object,
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
    public async Task CreateItemAsync_WithPassword_CreatesPasswordItem()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.GetUserPrivilegeAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(Privilege.Owner);
        _vaultServiceMock.Setup(x => x.IsVaultAccessibleAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(true);

        var dto = new CreateVaultItemDTO
        {
            VaultId = _testVault.Id,
            ItemType = ItemType.Password,
            Title = "Password Item",
            Username = "testuser",
            Password = "password123",
            WebsiteUrl = "https://example.com"
        };

        _encryptionServiceMock.Setup(x => x.Encrypt(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((plain, key) => $"encrypted_{plain}");

        // Act
        var result = await _vaultItemService.CreateItemAsync(dto, _testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ItemType.Password, result.ItemType);
        Assert.NotNull(result.Password);
        Assert.Equal("testuser", result.Password.Username);
    }

    [Fact]
    public async Task CreateItemAsync_WithLink_CreatesLinkItem()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.GetUserPrivilegeAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(Privilege.Owner);
        _vaultServiceMock.Setup(x => x.IsVaultAccessibleAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(true);

        var dto = new CreateVaultItemDTO
        {
            VaultId = _testVault.Id,
            ItemType = ItemType.Link,
            Title = "Link Item",
            Url = "https://example.com",
            LinkNotes = "Some notes"
        };

        _encryptionServiceMock.Setup(x => x.Encrypt(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((plain, key) => $"encrypted_{plain}");
        _encryptionServiceMock.Setup(x => x.Decrypt(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((cipher, key) => cipher.Replace("encrypted_", ""));

        // Act
        var result = await _vaultItemService.CreateItemAsync(dto, _testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ItemType.Link, result.ItemType);
        Assert.NotNull(result.Link);
        Assert.Equal("https://example.com", result.Link.Url);
    }

    [Fact]
    public async Task CreateItemAsync_WithCryptoWallet_CreatesCryptoWalletItem()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.GetUserPrivilegeAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(Privilege.Owner);
        _vaultServiceMock.Setup(x => x.IsVaultAccessibleAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(true);

        var dto = new CreateVaultItemDTO
        {
            VaultId = _testVault.Id,
            ItemType = ItemType.CryptoWallet,
            Title = "Crypto Wallet",
            WalletType = WalletType.SeedPhrase,
            PlatformName = "MetaMask",
            Blockchain = "Ethereum",
            PublicAddress = "0x123...",
            Secret = "seed phrase words"
        };

        _encryptionServiceMock.Setup(x => x.Encrypt(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((plain, key) => $"encrypted_{plain}");
        _encryptionServiceMock.Setup(x => x.Decrypt(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((cipher, key) => cipher.Replace("encrypted_", ""));

        // Act
        var result = await _vaultItemService.CreateItemAsync(dto, _testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ItemType.CryptoWallet, result.ItemType);
        Assert.NotNull(result.CryptoWallet);
        Assert.Equal(WalletType.SeedPhrase, result.CryptoWallet.WalletType);
    }

    [Fact]
    public async Task RestoreItemAsync_WithDeletedItem_RestoresItem()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.IsVaultAccessibleAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(true);
        _vaultServiceMock.Setup(x => x.CanEditVaultAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(true);

        var item = new VaultItem
        {
            VaultId = _testVault.Id,
            CreatedByUserId = _testUser.Id,
            ItemType = ItemType.Note,
            Title = "Deleted Item",
            Status = ItemStatus.Deleted,
            DeletedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        _dbContext.VaultItems.Add(item);
        
        var member = await _dbContext.VaultMembers.FirstAsync(m => m.VaultId == _testVault.Id);
        var visibility = new VaultItemVisibility
        {
            VaultItemId = item.Id,
            VaultMemberId = member.Id,
            Permission = ItemPermission.Edit
        };
        _dbContext.VaultItemVisibilities.Add(visibility);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _vaultItemService.RestoreItemAsync(item.Id, _testUser.Id);

        // Assert
        Assert.True(result);
        var restoredItem = await _dbContext.VaultItems.FindAsync(item.Id);
        Assert.NotNull(restoredItem);
        Assert.Equal(ItemStatus.Active, restoredItem.Status);
        Assert.Null(restoredItem.DeletedAt);
    }

    [Fact]
    public async Task RestoreItemAsync_WithExpiredItem_ReturnsFalse()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.IsVaultAccessibleAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(true);
        _vaultServiceMock.Setup(x => x.CanEditVaultAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(true);

        var item = new VaultItem
        {
            VaultId = _testVault.Id,
            CreatedByUserId = _testUser.Id,
            ItemType = ItemType.Note,
            Title = "Expired Item",
            Status = ItemStatus.Deleted,
            DeletedAt = DateTime.UtcNow.AddDays(-31), // More than 30 days
            CreatedAt = DateTime.UtcNow.AddDays(-50)
        };
        _dbContext.VaultItems.Add(item);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _vaultItemService.RestoreItemAsync(item.Id, _testUser.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserPermissionAsync_AsOwner_ReturnsEdit()
    {
        // Arrange
        var item = new VaultItem
        {
            VaultId = _testVault.Id,
            CreatedByUserId = _testUser.Id,
            ItemType = ItemType.Note,
            Title = "Test",
            Status = ItemStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.VaultItems.Add(item);
        
        var member = await _dbContext.VaultMembers.FirstAsync(m => m.VaultId == _testVault.Id);
        var visibility = new VaultItemVisibility
        {
            VaultItemId = item.Id,
            VaultMemberId = member.Id,
            Permission = ItemPermission.Edit
        };
        _dbContext.VaultItemVisibilities.Add(visibility);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _vaultItemService.GetUserPermissionAsync(item.Id, _testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ItemPermission.Edit, result);
    }

    [Fact]
    public async Task CanEditItemAsync_WithEditPermission_ReturnsTrue()
    {
        // Arrange
        var item = new VaultItem
        {
            VaultId = _testVault.Id,
            CreatedByUserId = _testUser.Id,
            ItemType = ItemType.Note,
            Title = "Test",
            Status = ItemStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.VaultItems.Add(item);
        
        var member = await _dbContext.VaultMembers.FirstAsync(m => m.VaultId == _testVault.Id);
        var visibility = new VaultItemVisibility
        {
            VaultItemId = item.Id,
            VaultMemberId = member.Id,
            Permission = ItemPermission.Edit
        };
        _dbContext.VaultItemVisibilities.Add(visibility);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _vaultItemService.CanEditItemAsync(item.Id, _testUser.Id);

        // Assert
        Assert.True(result);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

