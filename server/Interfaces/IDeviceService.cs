using server.Models;

namespace server.Interfaces;

public interface IDeviceService
{
    Task<UserDevice> GetOrCreateDeviceAsync(User user, string deviceIdentifier);
}