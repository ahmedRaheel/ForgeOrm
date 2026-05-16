using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Security;

public sealed record ForgeSqlSafetyResult(bool IsSafe, IReadOnlyList<string> Violations);

public interface IForgeSqlSecurityValidator
{
    ForgeSqlSafetyResult Validate(string sql, bool allowDdl = false, bool allowDangerous = false);
}

public sealed class ForgeSqlSecurityValidator : IForgeSqlSecurityValidator
{
    private static readonly string[] DangerousTokens = ["DROP ", "TRUNCATE ", "EXEC ", "EXECUTE ", "xp_", "sp_configure", "--", "/*", "*/"];
    private static readonly string[] DdlTokens = ["CREATE ", "ALTER ", "DROP "];

    /// <summary>
    /// Initializes or executes the Validate operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="allowDdl">The allowDdl value.</param>
    /// <param name="allowDangerous">The allowDangerous value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSqlSafetyResult Validate(string sql, bool allowDdl = false, bool allowDangerous = false)
    {
        var upper = $" {sql.ToUpperInvariant()} ";
        var violations = new List<string>();
        if (!allowDangerous)
            violations.AddRange(DangerousTokens.Where(token => upper.Contains(token)).Select(token => $"Dangerous SQL token detected: {token.Trim()}"));
        if (!allowDdl)
            violations.AddRange(DdlTokens.Where(token => upper.Contains(token)).Select(token => $"DDL token requires explicit permission: {token.Trim()}"));
        if (upper.Count(c => c == ';') > 1) violations.Add("Multiple SQL statements are not allowed by default.");
        return new ForgeSqlSafetyResult(violations.Count == 0, violations);
    }
}

public interface IForgeDataMasker
{
    string MaskEmail(string email);
    string MaskPhone(string phone);
    string Mask(string value, int visibleStart = 2, int visibleEnd = 2);
}

public sealed class ForgeDataMasker : IForgeDataMasker
{
    /// <summary>
    /// Initializes or executes the MaskEmail operation.
    /// </summary>
    /// <param name="email">The email value.</param>
    /// <returns>The operation result.</returns>
    public string MaskEmail(string email)
    {
        var parts = email.Split('@');
        return parts.Length == 2 ? $"{Mask(parts[0], 1, 1)}@{parts[1]}" : Mask(email);
    }
    /// <summary>
    /// Initializes or executes the MaskPhone operation.
    /// </summary>
    /// <param name="phone">The phone value.</param>
    /// <returns>The operation result.</returns>
    public string MaskPhone(string phone) => Mask(phone, 2, 2);
    /// <summary>
    /// Initializes or executes the Mask operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="visibleStart">The visibleStart value.</param>
    /// <param name="visibleEnd">The visibleEnd value.</param>
    /// <returns>The operation result.</returns>
    public string Mask(string value, int visibleStart = 2, int visibleEnd = 2)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.Length <= visibleStart + visibleEnd) return new string('*', value.Length);
        return value[..visibleStart] + new string('*', value.Length - visibleStart - visibleEnd) + value[^visibleEnd..];
    }
}

public interface IForgeColumnEncryptor
{
    string EncryptToBase64(string plainText, string key);
    string DecryptFromBase64(string cipherText, string key);
}

public sealed class ForgeAesColumnEncryptor : IForgeColumnEncryptor
{
    /// <summary>
    /// Initializes or executes the EncryptToBase64 operation.
    /// </summary>
    /// <param name="plainText">The plainText value.</param>
    /// <param name="key">The key value.</param>
    /// <returns>The operation result.</returns>
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
    /// Initializes or executes the DecryptFromBase64 operation.
    /// </summary>
    /// <param name="cipherText">The cipherText value.</param>
    /// <param name="key">The key value.</param>
    /// <returns>The operation result.</returns>
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

public static class ForgeSecurityServiceCollectionExtensions
{
    /// <summary>
    /// Initializes or executes the AddForgeSecurity operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The operation result.</returns>
    public static IServiceCollection AddForgeSecurity(this IServiceCollection services)
    {
        services.AddSingleton<IForgeSqlSecurityValidator, ForgeSqlSecurityValidator>();
        services.AddSingleton<IForgeDataMasker, ForgeDataMasker>();
        services.AddSingleton<IForgeColumnEncryptor, ForgeAesColumnEncryptor>();
        return services;
    }
}
public sealed record AuthorizeRequest
{
    public required string UserId { get; init; }
    public required string Resource { get; init; }
    public required string Action { get; init; }
    public string? TenantId { get; init; }
    public IReadOnlyList<string> Roles { get; init; }        = [];
    public IReadOnlyDictionary<string, string> Claims { get; init; }   = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTimeOffset RequestedAt { get; init; }  = DateTimeOffset.UtcNow;
    public CancellationToken CancellationToken { get; init; }
}
