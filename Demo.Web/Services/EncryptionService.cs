using System.Security.Cryptography;
using System.Text;

namespace Demo.Web.Services;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;
    private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        _logger = logger;

        // In production: Use Azure Key Vault or similar
        var encryptionKey = configuration["Encryption:Key"] ?? "MySecretKey12345MySecretKey12345"; // 32 bytes
        var encryptionIV = configuration["Encryption:IV"] ?? "MySecretIV123456"; // 16 bytes

        _key = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
        _iv = Encoding.UTF8.GetBytes(encryptionIV.PadRight(16).Substring(0, 16));

        _logger.LogInformation("?? Encryption Service initialized");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError($"? Encryption failed: {ex.Message}");
            throw;
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError($"? Decryption failed: {ex.Message}");
            throw;
        }
    }

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty");

        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    public bool VerifyPassword(string password, string hash)
    {
        var passwordHash = HashPassword(password);
        return passwordHash == hash;
    }
}
