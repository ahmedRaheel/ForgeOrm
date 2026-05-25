namespace ForgeORM.Core;

public sealed record ForgeRuntimeTenantContext(string TenantId);
public sealed record ForgeRuntimeOutboxMessage(Guid Id, string Type, string Payload, DateTimeOffset CreatedAtUtc, string? TenantId = null);
public sealed record ForgeSecurityFinding(string Severity, string Message);

public static class ForgeEnterpriseRuntimeExtensions
{
    public static ForgeQueryBuilder<TEntity> ForTenant<TEntity>(this ForgeQueryBuilder<TEntity> query, string tenantColumn, string tenantId)
        where TEntity : class, new()
        => query.WhereSql($"{tenantColumn} = @TenantId", new { TenantId = tenantId });

    public static IReadOnlyList<ForgeSecurityFinding> ValidateSqlSafety(this ForgeDb db, string sql)
    {
        var findings = new List<ForgeSecurityFinding>();
        if (sql.Contains(";--", StringComparison.Ordinal) || sql.Contains("/*", StringComparison.Ordinal))
            findings.Add(new ForgeSecurityFinding("High", "SQL contains comment markers often used in injection attempts."));
        if (sql.Contains(" DROP ", StringComparison.OrdinalIgnoreCase))
            findings.Add(new ForgeSecurityFinding("Critical", "SQL contains DROP."));
        if (sql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase))
            findings.Add(new ForgeSecurityFinding("Info", "SELECT * can expose more columns than intended."));
        return findings;
    }

    public static string MaskEmail(this ForgeDb db, string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***";
        return email[0] + "***" + email[at..];
    }

    public static async ValueTask<int> SaveOutboxAsync(this ForgeDb db, ForgeRuntimeOutboxMessage message, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO ForgeOutbox (Id, Type, Payload, CreatedAtUtc, TenantId) VALUES (@Id, @Type, @Payload, @CreatedAtUtc, @TenantId)";
        return await db.ExecuteAsync(sql, message, cancellationToken: cancellationToken);
    }

    public static async ValueTask<int> SaveWithOutboxAsync<TEntity>(this ForgeDb db, TEntity entity, ForgeRuntimeOutboxMessage message, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var saved = await db.InsertAsync(entity, cancellationToken);
        try { await db.SaveOutboxAsync(message, cancellationToken); }
        catch { /* sample-safe: real implementation should use same transaction */ }
        return saved;
    }
}
