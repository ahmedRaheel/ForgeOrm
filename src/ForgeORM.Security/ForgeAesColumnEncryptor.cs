using System.Security.Cryptography;
using System.Text;

namespace ForgeORM.Security;

public sealed class ForgeAesColumnEncryptor : IForgeColumnEncryptor
{
    /// <summary>
    /// Executes the EncryptToBase64 operation.
    /// </summary>
    /// <param name="plainText">The plainText value.</param>
    /// <param name="key">The key value.</param>
    /// <returns>The result of the EncryptToBase64 operation.</returns>
    public string EncryptToBase64(string plainText, string key)
    {
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var cipher = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(aes.IV.Concat(cipher).ToArray());
    }

    /// <summary>
    /// Executes the DecryptFromBase64 operation.
    /// </summary>
    /// <param name="cipherText">The cipherText value.</param>
    /// <param name="key">The key value.</param>
    /// <returns>The result of the DecryptFromBase64 operation.</returns>
    public string DecryptFromBase64(string cipherText, string key)
    {
        var payload = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        aes.IV = payload.Take(16).ToArray();
        using var decryptor = aes.CreateDecryptor();
        var cipher = payload.Skip(16).ToArray();
        var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }
}
