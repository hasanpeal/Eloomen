using server.Models;

namespace server.Interfaces;

public interface ITokenService
{
    string CreateToken(User user);
}