namespace ForgeORM.Security;

public interface IForgeColumnEncryptor
/// <summary>
/// Defines the EncryptToBase64 operation.
/// </summary>
/// <param name="plainText">The plainText value.</param>
/// <param name="key">The key value.</param>
/// <returns>The result of the EncryptToBase64 operation.</returns>
{
    /// <summary>
    /// Defines the EncryptToBase64 operation.
    /// </summary>
    /// <param name="plainText">The plainText value.</param>
    /// <param name="key">The key value.</param>
    /// <returns>The result of the EncryptToBase64 operation.</returns>
    string EncryptToBase64(string plainText, string key);
    /// <summary>
    /// Defines the DecryptFromBase64 operation.
    /// </summary>
    /// <param name="cipherText">The cipherText value.</param>
    /// <param name="key">The key value.</param>
    /// <returns>The result of the DecryptFromBase64 operation.</returns>
    string DecryptFromBase64(string cipherText, string key);
}
