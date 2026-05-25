using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ForgeORM.Core;

internal static class ForgeProviderAdo
{
    internal static class PropertyCache<T>
    {
        public static readonly (PropertyInfo Info, string ParamName, Type DeclaredType)[] Properties =
            typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && IsScalar(p.PropertyType))
                .Select(p => (p, "@" + p.Name, p.PropertyType))
                .ToArray();
    }

    public static ValueTask<int> ExecuteManyAsync<T>(
        DbConnection connection,
        string sql,
        IReadOnlyCollection<T> rows,
        CancellationToken cancellationToken)
    {
        return ForgeBulkExecutorFactory
            .Resolve(connection)
            .ExecuteManyAsync(connection, sql, rows, cancellationToken);
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
}
