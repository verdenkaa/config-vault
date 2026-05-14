using System.Security.Cryptography;
using System.Text;

namespace ConfigVault.Api.Services;

public interface IEncryptionService
{
    /// <summary>
    /// Зашифровать строку и вернуть Base64-строку, содержащую nonce + ciphertext + tag.
    /// </summary>
    string Encrypt(string plaintext);

    /// <summary>
    /// Расшифровать строку, полученную из Encrypt.
    /// </summary>
    string Decrypt(string encryptedBase64);
}

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _masterKey;

    public EncryptionService(IConfiguration configuration)
    {
        // В appsettings.json должен быть ключ "Encryption:MasterKey" в Base64 (32 байта после декодирования)
        var keyBase64 = configuration["Encryption:MasterKey"]
                        ?? throw new InvalidOperationException("Encryption:MasterKey is not configured.");
        _masterKey = Convert.FromBase64String(keyBase64);
        if (_masterKey.Length != 32)
            throw new InvalidOperationException("Encryption key must be 32 bytes for AES-256.");
    }

    public string Encrypt(string plaintext)
    {
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        // Генерируем уникальный nonce (12 байт для AES-GCM)
        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[16]; // аутентификационный тег

        using var aesGcm = new AesGcm(_masterKey);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Комбинируем nonce + ciphertext + tag и кодируем в Base64
        byte[] combined = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length + ciphertext.Length, tag.Length);

        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string encryptedBase64)
    {
        byte[] combined = Convert.FromBase64String(encryptedBase64);

        // Извлекаем nonce, ciphertext, tag
        byte[] nonce = combined.Take(12).ToArray();
        byte[] tag = combined.Skip(combined.Length - 16).ToArray();
        byte[] ciphertext = combined.Skip(12).Take(combined.Length - 28).ToArray();

        byte[] plaintext = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(_masterKey);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}