using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

internal static partial class ForgeGraphWriteHelpers
{
    public static List<PropertyInfo> GetInsertProperties(ForgeEntityShape shape, bool includeKey = false)
    {
        var key = shape.KeyProperty;

        return shape.ScalarProperties
            .Where(p => p.CanRead)
            .Where(p => !ForgeEntityShape.IsComputed(p))
            // Numeric identity keys are database generated and must never be sent in INSERT.
            // This prevents: Cannot insert explicit value for identity column ... IDENTITY_INSERT is OFF.
            .Where(p => key is null || !p.Name.Equals(key.Name, StringComparison.OrdinalIgnoreCase) || (includeKey && !IsDatabaseGeneratedIdentityKey(key)))
            .ToList();
    }

    private static bool IsDatabaseGeneratedIdentityKey(PropertyInfo key)
    {
        var keyType = Nullable.GetUnderlyingType(key.PropertyType) ?? key.PropertyType;
        return keyType == typeof(int)
            || keyType == typeof(long)
            || keyType == typeof(short);
    }

    public static List<PropertyInfo> GetUpdateProperties(ForgeEntityShape shape)
    {
        var key = shape.KeyProperty;

        return shape.ScalarProperties
            .Where(p => p.CanRead)
            .Where(p => !ForgeEntityShape.IsComputed(p))
            .Where(p => key is null || !p.Name.Equals(key.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static string BuildInsertSql(ForgeEntityShape shape, IReadOnlyList<PropertyInfo> props, bool includeScopeIdentity)
    {
        var key = shape.KeyProperty;
        var safeProps = props
            .Where(p => p.CanRead)
            .Where(p => key is null || !p.Name.Equals(key.Name, StringComparison.OrdinalIgnoreCase) || !IsDatabaseGeneratedIdentityKey(key))
            .ToArray();

        if (safeProps.Length == 0)
            throw new InvalidOperationException($"No insertable scalar columns were found for table {shape.TableName}.");

        var columns = string.Join(", ", safeProps.Select(ForgeEntityShape.ColumnName));
        var values = string.Join(", ", safeProps.Select(p => "@" + p.Name));
        var sql = $"INSERT INTO {shape.TableName} ({columns}) VALUES ({values})";

        if (includeScopeIdentity && key is not null && IsDatabaseGeneratedIdentityKey(key))
            sql += "; SELECT CAST(SCOPE_IDENTITY() AS int);";

        return sql;
    }

    public static Dictionary<string, object?> CreateParameterDictionary(IEnumerable<PropertyInfo> props, object entity)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in props)
            parameters[prop.Name] = NormalizeDatabaseValue(ForgeRuntimeAccessorCache.Get(prop, entity), prop);

        return parameters;
    }

    public static object? NormalizeDatabaseValue(object? value, PropertyInfo? property = null)
    {
        value = ForgeEnumConversion.ToDatabaseValue(value, property);

        if (value is DateTime dateTime)
        {
            if (dateTime == default || dateTime < new DateTime(1753, 1, 1))
                return DateTime.UtcNow;

            return dateTime;
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            if (dateTimeOffset == default)
                return DateTimeOffset.UtcNow;

            return dateTimeOffset;
        }

        return value;
    }
}
