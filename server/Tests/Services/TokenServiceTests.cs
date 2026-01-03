using Microsoft.Extensions.Configuration;
using server.Models;
using server.Services;
using server.Tests.Helpers;

namespace server.Tests.Services;

public class TokenServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDBContext _dbContext;
    private readonly TokenService _tokenService;
    private readonly User _testUser;

    public TokenServiceTests()
    {
        _configuration = TestHelpers.CreateTestConfiguration();
        _dbContext = TestHelpers.CreateInMemoryDbContext();
        _tokenService = new TokenService(_configuration, _dbContext);
        
        _testUser = TestHelpers.CreateTestUser();
        _dbContext.Users.Add(_testUser);
        _dbContext.SaveChanges();
    }

    [Fact]
    public void CreateToken_WithValidUser_ReturnsJwtToken()
    {
        // Act
        var token = _tokenService.CreateToken(_testUser);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        // JWT tokens have 3 parts separated by dots
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public void CreateToken_ContainsUserClaims()
    {
        // Act
        var token = _tokenService.CreateToken(_testUser);

        // Assert
        Assert.NotNull(token);
        // Token should be valid JWT format
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        
        Assert.Equal(_testUser.Id, jsonToken.Subject);
        Assert.Equal(_testUser.Email, jsonToken.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value);
        Assert.Equal(_testUser.UserName, jsonToken.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.GivenName)?.Value);
    }

    [Fact]
    public void CreateRefreshToken_ReturnsUniqueTokens()
    {
        // Act
        var token1 = _tokenService.CreateRefreshToken();
        var token2 = _tokenService.CreateRefreshToken();

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void CreateRefreshToken_ReturnsBase64String()
    {
        // Act
        var token = _tokenService.CreateRefreshToken();

        // Assert
        Assert.NotNull(token);
        // Verify it's valid base64
        var bytes = Convert.FromBase64String(token);
        Assert.True(bytes.Length > 0);
    }
}

