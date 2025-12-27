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

    public async Task<UserDevice> GetOrCreateDeviceAsync(
        string userId,
        string deviceIdentifier)
    {
        var device = await _dbContext.UserDevices
            .FirstOrDefaultAsync(d =>
                d.UserId == userId &&
                d.DeviceIdentifier == deviceIdentifier);

        if (device != null)
            return device;

        device = new UserDevice
        {
            UserId = userId,                 
            DeviceIdentifier = deviceIdentifier,
            IsVerified = false,
        };

        _dbContext.UserDevices.Add(device);
        return device;
    }

}