using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ForgeORM.Core;

internal static class ForgeProviderAdo
{
    internal static class PropertyCache<T>
    {
        public static readonly (PropertyInfo Info, string ParamName, Type DeclaredType)[] Properties = Build();

        private static (PropertyInfo Info, string ParamName, Type DeclaredType)[] Build()
        {
            var source = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (source.Length == 0)
                return Array.Empty<(PropertyInfo Info, string ParamName, Type DeclaredType)>();

            var buffer = new (PropertyInfo Info, string ParamName, Type DeclaredType)[source.Length];
            var count = 0;

            for (var i = 0; i < source.Length; i++)
            {
                var property = source[i];
                if (!property.CanRead || !IsScalar(property.PropertyType))
                    continue;

                buffer[count++] = (property, "@" + property.Name, property.PropertyType);
            }

            if (count == 0)
                return Array.Empty<(PropertyInfo Info, string ParamName, Type DeclaredType)>();

            if (count == buffer.Length)
                return buffer;

            var result = new (PropertyInfo Info, string ParamName, Type DeclaredType)[count];
            Array.Copy(buffer, result, count);
            return result;
        }
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
