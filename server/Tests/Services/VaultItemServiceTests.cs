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

public class VaultItemServiceTests : IDisposable
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

    public VaultItemServiceTests()
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
    public async Task GetVaultItemsAsync_AsOwner_ReturnsItems()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.GetUserPrivilegeAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(Privilege.Owner);
        _vaultServiceMock.Setup(x => x.IsVaultAccessibleAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(true);

        var item = new VaultItem
        {
            VaultId = _testVault.Id,
            CreatedByUserId = _testUser.Id,
            ItemType = ItemType.Note,
            Title = "Test Note",
            Status = ItemStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.VaultItems.Add(item);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _vaultItemService.GetVaultItemsAsync(_testVault.Id, _testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetVaultItemsAsync_WithoutAccess_ReturnsEmpty()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.GetUserPrivilegeAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync((Privilege?)null);

        // Act
        var result = await _vaultItemService.GetVaultItemsAsync(_testVault.Id, _testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetItemByIdAsync_AsOwner_ReturnsItem()
    {
        // Arrange
        var item = new VaultItem
        {
            VaultId = _testVault.Id,
            CreatedByUserId = _testUser.Id,
            ItemType = ItemType.Note,
            Title = "Test Note",
            Status = ItemStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.VaultItems.Add(item);
        
        var member = new VaultMember
        {
            VaultId = _testVault.Id,
            UserId = _testUser.Id,
            Privilege = Privilege.Owner,
            Status = MemberStatus.Active
        };
        _dbContext.VaultMembers.Add(member);
        
        var visibility = new VaultItemVisibility
        {
            VaultItemId = item.Id,
            VaultMemberId = member.Id,
            Permission = ItemPermission.Edit
        };
        _dbContext.VaultItemVisibilities.Add(visibility);
        await _dbContext.SaveChangesAsync();

        _encryptionServiceMock.Setup(x => x.Decrypt(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((cipher, key) => cipher);

        // Act
        var result = await _vaultItemService.GetItemByIdAsync(item.Id, _testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(item.Id, result.Id);
    }

    [Fact]
    public async Task CreateItemAsync_WithNote_CreatesNoteItem()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.GetUserPrivilegeAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(Privilege.Owner);
        _vaultServiceMock.Setup(x => x.IsVaultAccessibleAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(true);

        var member = new VaultMember
        {
            VaultId = _testVault.Id,
            UserId = _testUser.Id,
            Privilege = Privilege.Owner,
            Status = MemberStatus.Active
        };
        _dbContext.VaultMembers.Add(member);
        await _dbContext.SaveChangesAsync();

        var dto = new CreateVaultItemDTO
        {
            VaultId = _testVault.Id,
            ItemType = ItemType.Note,
            Title = "New Note",
            Description = "Note description",
            NoteContent = "Note content"
        };

        _encryptionServiceMock.Setup(x => x.Encrypt(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((plain, key) => $"encrypted_{plain}");

        // Act
        var result = await _vaultItemService.CreateItemAsync(dto, _testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Note", result.Title);
        Assert.Equal(ItemType.Note, result.ItemType);
        Assert.NotNull(result.Note);
    }

    [Fact]
    public async Task CreateItemAsync_WithoutVaultAccess_ThrowsUnauthorized()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.GetUserPrivilegeAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync((Privilege?)null);

        var dto = new CreateVaultItemDTO
        {
            VaultId = _testVault.Id,
            ItemType = ItemType.Note,
            Title = "New Note"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _vaultItemService.CreateItemAsync(dto, _testUser.Id));
    }

    [Fact]
    public async Task UpdateItemAsync_WithEditPermission_UpdatesItem()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.IsVaultAccessibleAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(true);

        var item = new VaultItem
        {
            VaultId = _testVault.Id,
            CreatedByUserId = _testUser.Id,
            ItemType = ItemType.Note,
            Title = "Original Title",
            Status = ItemStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.VaultItems.Add(item);
        
        var note = new VaultNote
        {
            VaultItemId = item.Id,
            EncryptedContent = "encrypted_content",
            ContentFormat = ContentFormat.PlainText
        };
        _dbContext.VaultNotes.Add(note);
        
        var member = new VaultMember
        {
            VaultId = _testVault.Id,
            UserId = _testUser.Id,
            Privilege = Privilege.Owner,
            Status = MemberStatus.Active
        };
        _dbContext.VaultMembers.Add(member);
        
        var visibility = new VaultItemVisibility
        {
            VaultItemId = item.Id,
            VaultMemberId = member.Id,
            Permission = ItemPermission.Edit
        };
        _dbContext.VaultItemVisibilities.Add(visibility);
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateVaultItemDTO
        {
            Title = "Updated Title",
            NoteContent = "Updated content"
        };

        _encryptionServiceMock.Setup(x => x.Encrypt(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((plain, key) => $"encrypted_{plain}");
        _encryptionServiceMock.Setup(x => x.Decrypt(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((cipher, key) => cipher.Replace("encrypted_", ""));

        // Act
        var result = await _vaultItemService.UpdateItemAsync(item.Id, dto, _testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
    }

    [Fact]
    public async Task DeleteItemAsync_WithEditPermission_DeletesItem()
    {
        // Arrange
        _vaultServiceMock.Setup(x => x.IsVaultAccessibleAsync(_testVault.Id, _testUser.Id))
            .ReturnsAsync(true);

        var item = new VaultItem
        {
            VaultId = _testVault.Id,
            CreatedByUserId = _testUser.Id,
            ItemType = ItemType.Note,
            Title = "To Delete",
            Status = ItemStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.VaultItems.Add(item);
        
        var member = new VaultMember
        {
            VaultId = _testVault.Id,
            UserId = _testUser.Id,
            Privilege = Privilege.Owner,
            Status = MemberStatus.Active
        };
        _dbContext.VaultMembers.Add(member);
        
        var visibility = new VaultItemVisibility
        {
            VaultItemId = item.Id,
            VaultMemberId = member.Id,
            Permission = ItemPermission.Edit
        };
        _dbContext.VaultItemVisibilities.Add(visibility);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _vaultItemService.DeleteItemAsync(item.Id, _testUser.Id);

        // Assert
        Assert.True(result);
        var deletedItem = await _dbContext.VaultItems.FindAsync(item.Id);
        Assert.NotNull(deletedItem);
        Assert.Equal(ItemStatus.Deleted, deletedItem.Status);
    }

    [Fact]
    public async Task CanViewItemAsync_WithPermission_ReturnsTrue()
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
        
        var member = new VaultMember
        {
            VaultId = _testVault.Id,
            UserId = _testUser.Id,
            Privilege = Privilege.Owner,
            Status = MemberStatus.Active
        };
        _dbContext.VaultMembers.Add(member);
        
        var visibility = new VaultItemVisibility
        {
            VaultItemId = item.Id,
            VaultMemberId = member.Id,
            Permission = ItemPermission.View
        };
        _dbContext.VaultItemVisibilities.Add(visibility);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _vaultItemService.CanViewItemAsync(item.Id, _testUser.Id);

        // Assert
        Assert.True(result);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

