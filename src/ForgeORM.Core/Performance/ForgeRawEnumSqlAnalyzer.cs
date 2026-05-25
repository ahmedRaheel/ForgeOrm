using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

internal static class ForgeRawEnumSqlAnalyzer
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool RequiresNormalization<T>(string sql, CommandType commandType)
    {
        if (commandType != CommandType.Text || string.IsNullOrWhiteSpace(sql))
            return false;

        var enumMap = ForgeORM.Core.Performance.ForgeRawEnumParameterMap<T>.Map;
        if (enumMap.Count == 0)
            return false;

        // Do this once during plan creation, not once per execution.
        // QueryById should not pay enum/raw-SQL rewriting cost when the SQL does not reference enum columns.
        foreach (var item in enumMap)
        {
            if (ContainsIdentifier(sql, item.Key))
                return true;
        }

        return false;
    }

    private static bool ContainsIdentifier(string sql, string identifier)
    {
        var index = 0;
        while ((index = sql.IndexOf(identifier, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var before = index == 0 ? '\0' : sql[index - 1];
            var afterIndex = index + identifier.Length;
            var after = afterIndex >= sql.Length ? '\0' : sql[afterIndex];

            if (!IsIdentifierChar(before) && !IsIdentifierChar(after))
                return true;

            index = afterIndex;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsIdentifierChar(char ch)
        => char.IsLetterOrDigit(ch) || ch == '_' || ch == '@' || ch == ':';
}
