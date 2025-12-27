using Microsoft.EntityFrameworkCore;
using server.Interfaces;
using server.Models;

namespace server.Services;

public class DeviceService: IDeviceService
{
    private readonly ApplicationDBContext _dbContext;

    public DeviceService(ApplicationDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserDevice> GetOrCreateDeviceAsync(User user, string deviceIdentifier)
    {
        var device = await _dbContext.UserDevices.FirstOrDefaultAsync(x=> x.UserId == user.Id && x.DeviceIdentifier == deviceIdentifier);
        if (device == null)
        {
            var newDevice = new UserDevice
            {
                IsVerified = false,
                DeviceIdentifier = deviceIdentifier,
                UserId = user.Id,
            };
            await _dbContext.UserDevices.AddAsync(newDevice);
            await _dbContext.SaveChangesAsync();
            return newDevice;
        }
        return device;
    }
}