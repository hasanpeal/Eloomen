using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using server.Interfaces;
using server.Models;

namespace server.Services;

public class TokenService: ITokenService
{
    private readonly IConfiguration _config;
    private readonly SymmetricSecurityKey _key;
    private readonly ApplicationDBContext _dbContext;
    
    public TokenService(IConfiguration configuration, ApplicationDBContext dbContext)
    {
        _config = configuration;
        _key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_config["Jwt:SigningKey"]!));
        _dbContext = dbContext;
    }

    public string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.UserName),
            // Revocation support
            new Claim("security_stamp", user.SecurityStamp)
        };

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:AccessTokenMinutes"])),
            SigningCredentials = creds,
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public async void CreateVerificationCode()
    {
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        var hash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(code))
        );

        

    }
}