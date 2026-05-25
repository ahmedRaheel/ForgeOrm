using System.Reflection;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>
/// SQL Server temporal table helpers. These methods are intentionally opt-in and do not affect normal query performance.
/// </summary>
public partial class ForgeDb
{
    public ValueTask<IReadOnlyList<T>> TemporalAllAsync<T>(
        string? whereSql = null,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var sql = BuildTemporalSelectSql<T>("FOR SYSTEM_TIME ALL", whereSql);
        return QueryAsync<T>(sql, parameters, cancellationToken: cancellationToken);
    }

    public ValueTask<IReadOnlyList<T>> TemporalAsOfAsync<T>(
        DateTime asOfUtc,
        string? whereSql = null,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var sql = BuildTemporalSelectSql<T>("FOR SYSTEM_TIME AS OF @TemporalAsOf", whereSql);
        var merged = MergeTemporalParameters(parameters, new Dictionary<string, object?>
        {
            ["TemporalAsOf"] = asOfUtc
        });
        return QueryAsync<T>(sql, merged, cancellationToken: cancellationToken);
    }

    public ValueTask<IReadOnlyList<T>> TemporalBetweenAsync<T>(
        DateTime fromUtc,
        DateTime toUtc,
        string? whereSql = null,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var sql = BuildTemporalSelectSql<T>("FOR SYSTEM_TIME BETWEEN @TemporalFrom AND @TemporalTo", whereSql);
        var merged = MergeTemporalParameters(parameters, new Dictionary<string, object?>
        {
            ["TemporalFrom"] = fromUtc,
            ["TemporalTo"] = toUtc
        });
        return QueryAsync<T>(sql, merged, cancellationToken: cancellationToken);
    }

    public ValueTask<IReadOnlyList<T>> TemporalContainedInAsync<T>(
        DateTime fromUtc,
        DateTime toUtc,
        string? whereSql = null,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var sql = BuildTemporalSelectSql<T>("FOR SYSTEM_TIME CONTAINED IN (@TemporalFrom, @TemporalTo)", whereSql);
        var merged = MergeTemporalParameters(parameters, new Dictionary<string, object?>
        {
            ["TemporalFrom"] = fromUtc,
            ["TemporalTo"] = toUtc
        });
        return QueryAsync<T>(sql, merged, cancellationToken: cancellationToken);
    }

    public ValueTask<T?> TemporalAsOfByIdAsync<T>(
        object id,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var keyColumn = ResolveKeyColumn<T>();
        var sql = BuildTemporalSelectSql<T>("FOR SYSTEM_TIME AS OF @TemporalAsOf", $"{keyColumn} = @Id");
        return QueryFirstOrDefaultAsync<T>(
            sql,
            new Dictionary<string, object?> { ["Id"] = id, ["TemporalAsOf"] = asOfUtc },
            cancellationToken: cancellationToken);
    }

    public string GenerateEnableTemporalSql<T>(
        string schema = "dbo",
        string? historyTable = null,
        string periodStartColumn = "ValidFrom",
        string periodEndColumn = "ValidTo")
    {
        var table = ResolveTableName<T>();
        historyTable ??= table + "History";
        return $"""
        ALTER TABLE {schema}.{table}
        ADD
            {periodStartColumn} DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
                CONSTRAINT DF_{table}_{periodStartColumn} DEFAULT SYSUTCDATETIME(),
            {periodEndColumn} DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL
                CONSTRAINT DF_{table}_{periodEndColumn} DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
            PERIOD FOR SYSTEM_TIME ({periodStartColumn}, {periodEndColumn});

        ALTER TABLE {schema}.{table}
        SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = {schema}.{historyTable}, DATA_CONSISTENCY_CHECK = ON));
        """;
    }

    public string GenerateDisableTemporalSql<T>(string schema = "dbo")
    {
        var table = ResolveTableName<T>();
        return $"ALTER TABLE {schema}.{table} SET (SYSTEM_VERSIONING = OFF);";
    }

    private static string BuildTemporalSelectSql<T>(string temporalClause, string? whereSql)
    {
        var table = ResolveTableName<T>();
        var columns = ResolveScalarColumns<T>();
        var sql = $"SELECT {columns} FROM {table} {temporalClause}";
        if (!string.IsNullOrWhiteSpace(whereSql))
            sql += " WHERE " + whereSql;
        return sql;
    }

    private static string ResolveTableName<T>()
        => typeof(T).GetCustomAttribute<ForgeTableAttribute>()?.Name ?? typeof(T).Name;

    private static string ResolveKeyColumn<T>()
    {
        var key = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(x => x.GetCustomAttribute<ForgeKeyAttribute>() is not null ||
                                 x.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        return key?.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? key?.Name ?? "Id";
    }

    private static string ResolveScalarColumns<T>()
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanRead && ForgeMaterializer.IsScalar(x.PropertyType))
            .Where(x => x.GetCustomAttribute<ForgeComputedAttribute>() is null)
            .Select(x => x.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return props.Length == 0 ? "*" : string.Join(", ", props);
    }

    private static object MergeTemporalParameters(object? original, Dictionary<string, object?> temporal)
    {
        if (original is null)
            return temporal;

        if (original is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            foreach (var pair in pairs)
                temporal[pair.Key] = pair.Value;
            return temporal;
        }

        foreach (var prop in original.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead))
            temporal[prop.Name] = ForgeRuntimeAccessorCache.Get(prop, original);

        return temporal;
    }
}
