using Microsoft.EntityFrameworkCore;
using server.Models;
using server.Services;
using server.Tests.Helpers;

namespace server.Tests.Services;

public class DeviceServiceTests : IDisposable
{
    private readonly ApplicationDBContext _dbContext;
    private readonly DeviceService _deviceService;
    private readonly User _testUser;

    public DeviceServiceTests()
    {
        _dbContext = TestHelpers.CreateInMemoryDbContext();
        _deviceService = new DeviceService(_dbContext);
        
        _testUser = TestHelpers.CreateTestUser();
        _dbContext.Users.Add(_testUser);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetOrCreateDeviceAsync_WithNewDevice_CreatesDevice()
    {
        // Arrange
        var deviceIdentifier = Guid.NewGuid().ToString();

        // Act
        var device = await _deviceService.GetOrCreateDeviceAsync(_testUser.Id, deviceIdentifier);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.NotNull(device);
        Assert.Equal(_testUser.Id, device.UserId);
        Assert.Equal(deviceIdentifier, device.DeviceIdentifier);
        Assert.False(device.IsVerified);

        var deviceInDb = await _dbContext.UserDevices.FirstOrDefaultAsync(d => d.Id == device.Id);
        Assert.NotNull(deviceInDb);
    }

    [Fact]
    public async Task GetOrCreateDeviceAsync_WithExistingDevice_ReturnsExisting()
    {
        // Arrange
        var deviceIdentifier = Guid.NewGuid().ToString();
        var existingDevice = new UserDevice
        {
            UserId = _testUser.Id,
            DeviceIdentifier = deviceIdentifier,
            IsVerified = true,
            VerifiedAt = DateTime.UtcNow
        };
        _dbContext.UserDevices.Add(existingDevice);
        await _dbContext.SaveChangesAsync();

        // Act
        var device = await _deviceService.GetOrCreateDeviceAsync(_testUser.Id, deviceIdentifier);

        // Assert
        Assert.NotNull(device);
        Assert.Equal(existingDevice.Id, device.Id);
        Assert.True(device.IsVerified);
    }

    [Fact]
    public async Task GetOrCreateDeviceAsync_WithDifferentUser_SameDeviceId_CreatesSeparateDevice()
    {
        // Arrange
        var otherUser = TestHelpers.CreateTestUser(email: "other@example.com", username: "otheruser");
        _dbContext.Users.Add(otherUser);
        await _dbContext.SaveChangesAsync();

        var deviceIdentifier = Guid.NewGuid().ToString();

        // Act
        var device1 = await _deviceService.GetOrCreateDeviceAsync(_testUser.Id, deviceIdentifier);
        var device2 = await _deviceService.GetOrCreateDeviceAsync(otherUser.Id, deviceIdentifier);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.NotEqual(device1.Id, device2.Id);
        Assert.Equal(_testUser.Id, device1.UserId);
        Assert.Equal(otherUser.Id, device2.UserId);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

