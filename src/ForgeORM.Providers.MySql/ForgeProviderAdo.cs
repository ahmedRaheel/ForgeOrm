using ForgeORM.Abstractions;
using ForgeORM.Core;
using MySqlConnector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static ForgeORM.Providers.MySql.ForgeProviderAdo;

namespace ForgeORM.Providers.MySql;

internal static class ForgeProviderAdo
{
    // Reusable static generic metadata cache handled by the runtime engine per variations of T
    internal static class PropertyCache<T>
    {
        public static readonly (PropertyInfo Info, string ParamName, Type DeclaredType)[] Properties =
            typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && IsScalar(p.PropertyType))
                .Select(p => (p, "@" + p.Name, p.PropertyType))
                .ToArray();
    }

    public static ValueTask<int> ExecuteManyAsync<T>(DbConnection connection, string sql, IReadOnlyCollection<T> rows, CancellationToken cancellationToken)
    {
        if (rows is null || rows.Count == 0)
            return new ValueTask<int>(0);

        var cachedProps = PropertyCache<T>.Properties;
        if (cachedProps.Length == 0)
            return new ValueTask<int>(0);

        return ExecuteManyInternalAsync(connection, sql, rows, cachedProps, cancellationToken);
    }

    private static async ValueTask<int> ExecuteManyInternalAsync<T>(
        DbConnection connection,
        string sql,
        IReadOnlyCollection<T> rows,
        (PropertyInfo Info, string ParamName, Type DeclaredType)[] cachedProps,
        CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var dbParameters = new DbParameter[cachedProps.Length];
        for (int i = 0; i < cachedProps.Length; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = cachedProps[i].ParamName;
            command.Parameters.Add(parameter);
            dbParameters[i] = parameter;
        }

        try { command.Prepare(); } catch { /* Resilient execution fallback */ }

        var total = 0;
        foreach (var row in rows)
        {
            for (int i = 0; i < cachedProps.Length; i++)
            {
                ref readonly var propMetadata = ref cachedProps[i];
                var rawValue = ForgeProviderAccessors.Get(propMetadata.Info, row!);
                dbParameters[i].Value = NormalizeValue(rawValue, propMetadata.DeclaredType) ?? DBNull.Value;
            }

            total += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsScalar(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual.IsPrimitive
               || actual.IsEnum
               || actual == typeof(string)
               || actual == typeof(Guid)
               || actual == typeof(decimal)
               || actual == typeof(DateTime)
               || actual == typeof(DateTimeOffset)
               || actual == typeof(DateOnly)
               || actual == typeof(TimeOnly)
               || actual == typeof(TimeSpan)
               || actual == typeof(byte[]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? NormalizeValue(object? value, Type declaredType)
    {
        if (value is null) return null;

        var actual = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (actual == typeof(DateTime))
        {
            var dateTime = (DateTime)value;
            return dateTime == default || dateTime < new DateTime(1753, 1, 1) ? DateTime.UtcNow : dateTime;
        }

        if (actual == typeof(DateTimeOffset))
        {
            var dto = (DateTimeOffset)value;
            return dto == default ? DateTimeOffset.UtcNow : dto;
        }

        return value;
    }
}
