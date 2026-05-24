using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Security;

//public interface IForgeSqlSecurityValidator
///// <summary>
///// Defines the Validate operation.
///// </summary>
///// <param name="sql">The sql value.</param>
///// <param name="allowDdl">The allowDdl value.</param>
///// <param name="allowDangerous">The allowDangerous value.</param>
///// <returns>The result of the Validate operation.</returns>
//{
//    /// <summary>
//    /// Defines the Validate operation.
//    /// </summary>
//    /// <param name="sql">The sql value.</param>
//    /// <param name="allowDdl">The allowDdl value.</param>
//    /// <param name="allowDangerous">The allowDangerous value.</param>
//    /// <returns>The result of the Validate operation.</returns>
//    ForgeSqlSafetyResult Validate(string sql, bool allowDdl = false, bool allowDangerous = false);
//}

//public sealed class ForgeSqlSecurityValidator : IForgeSqlSecurityValidator
//{
//    private static readonly string[] DangerousTokens = ["DROP ", "TRUNCATE ", "EXEC ", "EXECUTE ", "xp_", "sp_configure", "--", "/*", "*/"];
//    private static readonly string[] DdlTokens = ["CREATE ", "ALTER ", "DROP "];

//    /// <summary>
//    /// Executes the Validate operation.
//    /// </summary>
//    /// <param name="sql">The sql value.</param>
//    /// <param name="allowDdl">The allowDdl value.</param>
//    /// <param name="allowDangerous">The allowDangerous value.</param>
//    /// <returns>The result of the Validate operation.</returns>
//    public ForgeSqlSafetyResult Validate(string sql, bool allowDdl = false, bool allowDangerous = false)
//    {
//        var upper = $" {sql.ToUpperInvariant()} ";
//        var violations = new List<string>();
//        if (!allowDangerous)
//            violations.AddRange(DangerousTokens.Where(token => upper.Contains(token)).Select(token => $"Dangerous SQL token detected: {token.Trim()}"));
//        if (!allowDdl)
//            violations.AddRange(DdlTokens.Where(token => upper.Contains(token)).Select(token => $"DDL token requires explicit permission: {token.Trim()}"));
//        if (upper.Count(c => c == ';') > 1) violations.Add("Multiple SQL statements are not allowed by default.");
//        return new ForgeSqlSafetyResult(violations.Count == 0, violations);
//    }
//}

//public interface IForgeDataMasker
///// <summary>
///// Defines the MaskEmail operation.
///// </summary>
///// <param name="email">The email value.</param>
///// <returns>The result of the MaskEmail operation.</returns>
//{
//    /// <summary>
//    /// Defines the MaskEmail operation.
//    /// </summary>
//    /// <param name="email">The email value.</param>
//    /// <returns>The result of the MaskEmail operation.</returns>
//    string MaskEmail(string email);
//    /// <summary>
//    /// Defines the MaskPhone operation.
//    /// </summary>
//    /// <param name="phone">The phone value.</param>
//    /// <returns>The result of the MaskPhone operation.</returns>
//    string MaskPhone(string phone);
//    /// <summary>
//    /// Defines the Mask operation.
//    /// </summary>
//    /// <param name="value">The value value.</param>
//    /// <param name="visibleStart">The visibleStart value.</param>
//    /// <param name="visibleEnd">The visibleEnd value.</param>
//    /// <returns>The result of the Mask operation.</returns>
//    string Mask(string value, int visibleStart = 2, int visibleEnd = 2);
//}

//public sealed class ForgeDataMasker : IForgeDataMasker
//{
//    /// <summary>
//    /// Executes the MaskEmail operation.
//    /// </summary>
//    /// <param name="email">The email value.</param>
//    /// <returns>The result of the MaskEmail operation.</returns>
//    public string MaskEmail(string email)
//    {
//        var parts = email.Split('@');
//        return parts.Length == 2 ? $"{Mask(parts[0], 1, 1)}@{parts[1]}" : Mask(email);
//    }
//    /// <summary>
//    /// Executes the MaskPhone operation.
//    /// </summary>
//    /// <param name="phone">The phone value.</param>
//    /// <returns>The result of the MaskPhone operation.</returns>
//    public string MaskPhone(string phone) => Mask(phone, 2, 2);
//    /// <summary>
//    /// Executes the Mask operation.
//    /// </summary>
//    /// <param name="value">The value value.</param>
//    /// <param name="visibleStart">The visibleStart value.</param>
//    /// <param name="visibleEnd">The visibleEnd value.</param>
//    /// <returns>The result of the Mask operation.</returns>
//    public string Mask(string value, int visibleStart = 2, int visibleEnd = 2)
//    {
//        if (string.IsNullOrEmpty(value)) return value;
//        if (value.Length <= visibleStart + visibleEnd) return new string('*', value.Length);
//        return value[..visibleStart] + new string('*', value.Length - visibleStart - visibleEnd) + value[^visibleEnd..];
//    }
//}

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
