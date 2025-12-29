using System.Security.Cryptography;
using System.Text;
using server.Interfaces;

namespace server.Services;

public class EncryptionService : IEncryptionService
{
    private readonly IConfiguration _configuration;

    public EncryptionService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Encrypt(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        using var aes = Aes.Create();
        aes.Key = DeriveKey(key, 32); // 256-bit key
        aes.IV = DeriveKey(key + "IV", 16); // 128-bit IV
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public string Decrypt(string cipherText, string key)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = DeriveKey(key, 32);
            aes.IV = DeriveKey(key + "IV", 16);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var cipherBytes = Convert.FromBase64String(cipherText);
            using var msDecrypt = new MemoryStream(cipherBytes);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
        catch
        {
            throw new CryptographicException("Failed to decrypt data. Invalid key or corrupted data.");
        }
    }

    public string GenerateKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var keyBytes = new byte[32];
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    private byte[] DeriveKey(string input, int keySize)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        var key = new byte[keySize];
        Array.Copy(hash, key, Math.Min(hash.Length, keySize));
        return key;
    }
}

