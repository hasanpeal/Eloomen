using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using server.Models;

namespace server.Tests.Helpers;

public static class TestHelpers
{
    public static ApplicationDBContext CreateInMemoryDbContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDBContext(options);
    }

    public static IConfiguration CreateTestConfiguration()
    {
        var config = new Dictionary<string, string>
        {
            { "Jwt:SigningKey", "test-signing-key-that-is-at-least-64-characters-long-for-hmac-sha512-algorithm-requirement" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:AccessTokenMinutes", "15" },
            { "Jwt:RefreshTokenDays", "30" },
            { "App:BaseUrl", "http://localhost:3000" },
            { "App:EmailVerificationPath", "/verify-email" },
            { "App:DeviceVerificationPath", "/verify-device" },
            { "App:PasswordResetPath", "/reset-password" },
            { "App:VerificationCodeExpiration:EmailVerificationMinutes", "30" },
            { "App:VerificationCodeExpiration:DeviceVerificationMinutes", "15" },
            { "App:VerificationCodeExpiration:PasswordResetMinutes", "60" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();
    }

    public static User CreateTestUser(string? id = null, string email = "test@example.com", string username = "testuser")
    {
        return new User
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Email = email,
            UserName = username,
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }

    public static Vault CreateTestVault(string ownerId, string name = "Test Vault", string description = "Test Description")
    {
        return new Vault
        {
            OwnerId = ownerId,
            OriginalOwnerId = ownerId,
            Name = name,
            Description = description,
            Status = VaultStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }
}

