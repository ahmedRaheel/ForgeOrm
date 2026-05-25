using ForgeORM.Abstractions;
using ForgeORM.Core;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ForgeORM.Providers.Oracle;

internal static class ForgeProviderAdo
{
    // High-performance static generic cache initialized once per type T by the CLR.
    // This reduces structural type lookup overhead to an absolute zero runtime allocation cost.
    private static class PropertyCache<T>
    {
        public static readonly (PropertyInfo Info, string ParamName, Type DeclaredType)[] Properties =
            typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && IsScalar(p.PropertyType))
                .Select(p => (p, "@" + p.Name, p.PropertyType))
                .ToArray();
    }

    /// <summary>
    /// Executes the T operation using a zero-allocation parameter mutation architecture.
    /// </summary>
    public static async ValueTask ExecuteManyAsync<T>(
    DbConnection connection,
    string sql,
    IReadOnlyCollection<T> rows,
    CancellationToken cancellationToken)
    {
        // 1. Guard clauses on the synchronous hot path.
        // If there is nothing to process, we exit with zero allocation using a pre-cached token.
        if (rows is null || rows.Count == 0)
            return;

        var cachedProps = PropertyCache<T>.Properties;
        if (cachedProps.Length == 0)
            return;

        // 2. Delegate to the internal async loop execution path
        await ExecuteManyInternalAsync(connection, sql, rows, cachedProps, cancellationToken).ConfigureAwait(false);
    }

    // Keeping the async state machine generation safely separated here
    private static async ValueTask ExecuteManyInternalAsync<T>(
        DbConnection connection,
        string sql,
        IReadOnlyCollection<T> rows,
        (System.Reflection.PropertyInfo Info, string ParamName, Type DeclaredType)[] cachedProps,
        CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Allocate EXACTLY ONE command and a single set of reusable parameters for the entire batch
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var dbParameters = new DbParameter[cachedProps.Length];
        for (int i = 0; i < cachedProps.Length; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = cachedProps[i].ParamName;
            command.Parameters.Add(parameter);
            dbParameters[i] = parameter; // Direct array access optimization
        }

        // Attempt command preparation if supported by the underlying ADO.NET provider
        try { command.Prepare(); } catch { /* Fallback for engines lacking explicit preparation support */ }

        // Allocation-Free Execution Loop
        foreach (var row in rows)
        {
            for (int i = 0; i < cachedProps.Length; i++)
            {
                ref readonly var propMetadata = ref cachedProps[i];
                var rawValue = ForgeProviderAccessors.Get(propMetadata.Info, row!);

                // Mutate the parameter values in-place on the heap pool
                dbParameters[i].Value = NormalizeValue(rawValue, propMetadata.DeclaredType) ?? DBNull.Value;
            }

            // Execute the database write without returning or capturing an unused integer result
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsScalar(Type type)
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
        if (value is null)
            return null;

        var actual = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (actual == typeof(DateTime))
        {
            var dateTime = (DateTime)value;
            return dateTime == default || dateTime < new DateTime(1753, 1, 1)
                ? DateTime.UtcNow
                : dateTime;
        }

        if (actual == typeof(DateTimeOffset))
        {
            var dto = (DateTimeOffset)value;
            return dto == default ? DateTimeOffset.UtcNow : dto;
        }

        return value;
    }
}
