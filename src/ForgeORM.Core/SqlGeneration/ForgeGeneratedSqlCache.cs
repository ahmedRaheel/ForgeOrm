using System.Collections.Concurrent;
using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>
/// Caches generated CRUD SQL per entity/provider. Source-generated providers can pre-register equivalent SQL;
/// RuntimeEmit uses this cache after metadata has been resolved once.
/// </summary>
public static class ForgeGeneratedSqlCache
{
    private static readonly ConcurrentDictionary<ForgeGeneratedSqlKey, ForgeGeneratedSqlPlan> Cache = new();

    public static ForgeGeneratedSqlPlan GetOrAdd(IForgeEntityMetadataResolver metadata, Type entityType, string providerName)
    {
        var key = new ForgeGeneratedSqlKey(entityType, providerName);
        return Cache.GetOrAdd(key, _ => Build(metadata.Resolve(entityType), providerName));
    }

    private static ForgeGeneratedSqlPlan Build(ForgeEntityMetadata meta, string providerName)
    {
        var insertable = meta.Properties.Where(x => !x.IsComputed && !x.IsKey).ToArray();
        var updateable = meta.Properties.Where(x => !x.IsComputed && !x.IsKey).ToArray();
        var table = Quote(meta.TableName, providerName);
        var key = Quote(meta.KeyColumn, providerName);

        var insertColumns = string.Join(", ", insertable.Select(x => Quote(x.ColumnName, providerName)));
        var insertValues = string.Join(", ", insertable.Select(x => "@" + x.PropertyName));
        var updateSet = string.Join(", ", updateable.Select(x => Quote(x.ColumnName, providerName) + " = @" + x.PropertyName));
        var selectColumns = meta.Properties.Count == 0
            ? "*"
            : string.Join(", ", meta.Properties.Where(x => !x.IsComputed).Select(x => Quote(x.ColumnName, providerName)));

        return new ForgeGeneratedSqlPlan(
            SelectById: $"SELECT {ProviderTopOne(providerName)} {selectColumns} FROM {table} WHERE {key} = @Id{ProviderLimitOne(providerName)}",
            SelectAll: $"SELECT {selectColumns} FROM {table}",
            Insert: $"INSERT INTO {table} ({insertColumns}) VALUES ({insertValues})",
            Update: $"UPDATE {table} SET {updateSet} WHERE {key} = @Id",
            Delete: $"DELETE FROM {table} WHERE {key} = @Id");
    }

    private static string ProviderTopOne(string providerName)
        => providerName.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) || providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase)
            ? "TOP 1"
            : string.Empty;

    private static string ProviderLimitOne(string providerName)
        => providerName.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) || providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : " LIMIT 1";

    private static string Quote(string name, string providerName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("SQL identifier cannot be empty.", nameof(name));

        if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) || providerName.Contains("Postgre", StringComparison.OrdinalIgnoreCase))
            return name.Contains('.') ? string.Join('.', name.Split('.').Select(x => '"' + x.Replace("\"", "\"\"") + '"')) : '"' + name.Replace("\"", "\"\"") + '"';

        return name.Contains('.') ? string.Join('.', name.Split('.').Select(x => '[' + x.Replace("]", "]]") + ']')) : '[' + name.Replace("]", "]]") + ']';
    }
}

public readonly record struct ForgeGeneratedSqlKey(Type EntityType, string ProviderName);
public sealed record ForgeGeneratedSqlPlan(string SelectById, string SelectAll, string Insert, string Update, string Delete);
