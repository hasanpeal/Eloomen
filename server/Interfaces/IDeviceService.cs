using server.Models;

namespace server.Interfaces;

public interface IDeviceService
{
    Task<UserDevice> GetOrCreateDeviceAsync(string userId, string deviceIdentifier);
}