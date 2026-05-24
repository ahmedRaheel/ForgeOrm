using System.Buffers;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;

namespace ForgeORM.Core;

/// <summary>
/// Provider-native collection parameter binding. All query APIs reach this through the compiled binder,
/// so split queries, raw SQL and SQL builder IN predicates behave the same way.
/// </summary>
internal static class ForgeProviderNativeCollectionBinder
{
    public static void BindInList(DbCommand command, string logicalName, IEnumerable values)
    {
        var provider = command.Connection?.GetType().FullName ?? command.Connection?.GetType().Name ?? string.Empty;

        if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            BindPostgreSqlAny(command, logicalName, values);
            return;
        }

        // SQL Server/MySQL/SQLite fallback: expanded scalar parameters. SQL Server TVP can be enabled later
        // when TypeName/table-type conventions are configured, but expanded params are safe and universal.
        BindExpanded(command, logicalName, values);
    }

    private static void BindPostgreSqlAny(DbCommand command, string logicalName, IEnumerable values)
    {
        var clean = logicalName.TrimStart('@', ':');
        RemoveParameterFamily(command, clean);
        var list = new List<object?>();
        foreach (var value in values)
            list.Add(value);

        var parameter = command.CreateParameter();
        parameter.ParameterName = ForgeParameterBinderCompiler.NormalizeParameterName(clean);
        parameter.Value = list.ToArray();
        command.Parameters.Add(parameter);

        command.CommandText = ReplaceInSyntaxForPostgres(command.CommandText, clean);
    }

    private static string ReplaceInSyntaxForPostgres(string sql, string cleanName)
    {
        var token = "@" + cleanName;
        return sql.Replace("IN " + token, "= ANY(" + token + ")", StringComparison.OrdinalIgnoreCase)
                  .Replace("IN (" + token + ")", "= ANY(" + token + ")", StringComparison.OrdinalIgnoreCase);
    }

    private static void BindExpanded(DbCommand command, string logicalName, IEnumerable values)
    {
        var cleanName = logicalName.TrimStart('@', ':');
        RemoveParameterFamily(command, cleanName);

        var capacity = values is ICollection collection ? Math.Max(collection.Count, 1) : 8;
        var rentedNames = ArrayPool<string>.Shared.Rent(capacity);
        var count = 0;
        try
        {
            foreach (var item in values)
            {
                if (count == rentedNames.Length)
                {
                    var expanded = ArrayPool<string>.Shared.Rent(rentedNames.Length * 2);
                    Array.Copy(rentedNames, expanded, rentedNames.Length);
                    ArrayPool<string>.Shared.Return(rentedNames, clearArray: true);
                    rentedNames = expanded;
                }

                var name = ForgeParameterBinderCompiler.NormalizeParameterName(cleanName + count.ToString(CultureInfo.InvariantCulture));
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = item ?? DBNull.Value;
                command.Parameters.Add(parameter);
                rentedNames[count++] = name;
            }

            var replacement = count == 0
                ? "(SELECT 1 WHERE 1 = 0)"
                : BuildExpandedParameterList(rentedNames.AsSpan(0, count));
            command.CommandText = ReplaceParameterToken(command.CommandText, cleanName, replacement);
        }
        finally
        {
            ArrayPool<string>.Shared.Return(rentedNames, clearArray: true);
        }
    }

    private static string BuildExpandedParameterList(ReadOnlySpan<string> names)
    {
        var length = 2;
        for (var i = 0; i < names.Length; i++)
            length += names[i].Length + (i == 0 ? 0 : 2);

        var builder = new StringBuilder(length);
        builder.Append('(');
        for (var i = 0; i < names.Length; i++)
        {
            if (i != 0)
                builder.Append(", ");
            builder.Append(names[i]);
        }
        builder.Append(')');
        return builder.ToString();
    }

    internal static void RemoveParameterFamily(DbCommand command, string logicalName)
    {
        var normalized = logicalName.TrimStart('@', ':');
        for (var i = command.Parameters.Count - 1; i >= 0; i--)
        {
            if (command.Parameters[i] is not DbParameter parameter) continue;
            var current = parameter.ParameterName.TrimStart('@', ':');
            if (string.Equals(current, normalized, StringComparison.OrdinalIgnoreCase) || IsExpandedParameterName(current, normalized))
                command.Parameters.RemoveAt(i);
        }
    }

    private static bool IsExpandedParameterName(string current, string logicalName)
    {
        if (!current.StartsWith(logicalName, StringComparison.OrdinalIgnoreCase)) return false;
        if (current.Length == logicalName.Length) return true;
        var c = current[logicalName.Length];
        return char.IsDigit(c) || c == '_';
    }

    private static string ReplaceParameterToken(string sql, string parameterName, string replacement)
    {
        var atName = "@" + parameterName;
        var colonName = ":" + parameterName;
        return ReplaceToken(ReplaceToken(sql, atName, replacement), colonName, replacement);
    }

    private static string ReplaceToken(string sql, string token, string replacement)
    {
        var index = 0;
        StringBuilder? builder = null;
        var last = 0;
        while ((index = sql.IndexOf(token, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var end = index + token.Length;
            if (end < sql.Length && (char.IsLetterOrDigit(sql[end]) || sql[end] == '_'))
            {
                index = end;
                continue;
            }
            builder ??= new StringBuilder(sql.Length + replacement.Length);
            builder.Append(sql, last, index - last);
            builder.Append(replacement);
            last = end;
            index = end;
        }
        if (builder is null) return sql;
        builder.Append(sql, last, sql.Length - last);
        return builder.ToString();
    }
}
