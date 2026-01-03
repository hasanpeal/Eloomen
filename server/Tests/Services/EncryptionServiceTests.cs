using Microsoft.Extensions.Configuration;
using server.Services;
using server.Tests.Helpers;

namespace server.Tests.Services;

public class EncryptionServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly EncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        _configuration = TestHelpers.CreateTestConfiguration();
        _encryptionService = new EncryptionService(_configuration);
    }

    [Fact]
    public void Encrypt_WithValidInput_ReturnsEncryptedString()
    {
        // Arrange
        var plainText = "Hello, World!";
        var key = _encryptionService.GenerateKey();

        // Act
        var encrypted = _encryptionService.Encrypt(plainText, key);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        Assert.NotEqual(plainText, encrypted);
    }

    [Fact]
    public void Encrypt_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var plainText = "";
        var key = _encryptionService.GenerateKey();

        // Act
        var encrypted = _encryptionService.Encrypt(plainText, key);

        // Assert
        Assert.Equal("", encrypted);
    }

    [Fact]
    public void Decrypt_WithValidEncryptedData_ReturnsOriginalText()
    {
        // Arrange
        var originalText = "Secret message 123";
        var key = _encryptionService.GenerateKey();
        var encrypted = _encryptionService.Encrypt(originalText, key);

        // Act
        var decrypted = _encryptionService.Decrypt(encrypted, key);

        // Assert
        Assert.Equal(originalText, decrypted);
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsCryptographicException()
    {
        // Arrange
        var originalText = "Secret message";
        var correctKey = _encryptionService.GenerateKey();
        var wrongKey = _encryptionService.GenerateKey();
        var encrypted = _encryptionService.Encrypt(originalText, correctKey);

        // Act & Assert
        Assert.Throws<System.Security.Cryptography.CryptographicException>(() =>
            _encryptionService.Decrypt(encrypted, wrongKey));
    }

    [Fact]
    public void Decrypt_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var cipherText = "";
        var key = _encryptionService.GenerateKey();

        // Act
        var decrypted = _encryptionService.Decrypt(cipherText, key);

        // Assert
        Assert.Equal("", decrypted);
    }

    [Fact]
    public void GenerateKey_ReturnsUniqueKeys()
    {
        // Act
        var key1 = _encryptionService.GenerateKey();
        var key2 = _encryptionService.GenerateKey();

        // Assert
        Assert.NotNull(key1);
        Assert.NotNull(key2);
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateKey_ReturnsBase64String()
    {
        // Act
        var key = _encryptionService.GenerateKey();

        // Assert
        Assert.NotNull(key);
        // Base64 strings are typically longer and contain specific characters
        Assert.True(key.Length > 0);
        // Verify it's valid base64
        var bytes = Convert.FromBase64String(key);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_WorksWithSpecialCharacters()
    {
        // Arrange
        var specialText = "Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?`~";
        var key = _encryptionService.GenerateKey();

        // Act
        var encrypted = _encryptionService.Encrypt(specialText, key);
        var decrypted = _encryptionService.Decrypt(encrypted, key);

        // Assert
        Assert.Equal(specialText, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_WorksWithUnicode()
    {
        // Arrange
        var unicodeText = "Unicode: 你好世界 🌍";
        var key = _encryptionService.GenerateKey();

        // Act
        var encrypted = _encryptionService.Encrypt(unicodeText, key);
        var decrypted = _encryptionService.Decrypt(encrypted, key);

        // Assert
        Assert.Equal(unicodeText, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_WorksWithLongText()
    {
        // Arrange
        var longText = new string('A', 10000);
        var key = _encryptionService.GenerateKey();

        // Act
        var encrypted = _encryptionService.Encrypt(longText, key);
        var decrypted = _encryptionService.Decrypt(encrypted, key);

        // Assert
        Assert.Equal(longText, decrypted);
    }
}

