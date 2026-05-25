using System.Collections;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace ForgeORM.DataFrame;

internal static class ForgeFrameQueryExpression
{
    private static readonly Regex BinaryExpression = new(
        "^\\s*(?<column>[A-Za-z_][A-Za-z0-9_ .]*)\\s*(?<op>==|=|!=|>=|<=|>|<|contains|startswith|endswith)\\s*(?<value>.+?)\\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static Func<IReadOnlyDictionary<string, object?>, bool> Compile(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return _ => true;

        var parts = Regex.Split(expression, "\\s+and\\s+", RegexOptions.IgnoreCase)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(CompileSingle)
            .ToArray();
        return row => parts.All(predicate => predicate(row));
    }

    private static Func<IReadOnlyDictionary<string, object?>, bool> CompileSingle(string expression)
    {
        var match = BinaryExpression.Match(expression);
        if (!match.Success)
            throw new ArgumentException($"Unsupported dataframe query expression: {expression}", nameof(expression));

        var column = match.Groups["column"].Value.Trim();
        var op = match.Groups["op"].Value.Trim().ToLowerInvariant();
        var expected = ParseLiteral(match.Groups["value"].Value.Trim());
        return row => Compare(ForgeDataFrame.Get(row, column), expected, op);
    }

    private static object? ParseLiteral(string text)
    {
        if ((text.StartsWith("'", StringComparison.Ordinal) && text.EndsWith("'", StringComparison.Ordinal)) ||
            (text.StartsWith("\"", StringComparison.Ordinal) && text.EndsWith("\"", StringComparison.Ordinal)))
            return text[1..^1];
        if (string.Equals(text, "null", StringComparison.OrdinalIgnoreCase))
            return null;
        if (bool.TryParse(text, out var b))
            return b;
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;
        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dto))
            return dto;
        return text;
    }

    private static bool Compare(object? actual, object? expected, string op)
    {
        if (op is "contains" or "startswith" or "endswith")
        {
            var a = Convert.ToString(actual, CultureInfo.InvariantCulture) ?? string.Empty;
            var e = Convert.ToString(expected, CultureInfo.InvariantCulture) ?? string.Empty;
            return op switch
            {
                "contains" => a.Contains(e, StringComparison.OrdinalIgnoreCase),
                "startswith" => a.StartsWith(e, StringComparison.OrdinalIgnoreCase),
                _ => a.EndsWith(e, StringComparison.OrdinalIgnoreCase)
            };
        }

        var cmp = CompareValue(actual, expected);
        return op switch
        {
            "=" or "==" => cmp == 0,
            "!=" => cmp != 0,
            ">" => cmp > 0,
            ">=" => cmp >= 0,
            "<" => cmp < 0,
            "<=" => cmp <= 0,
            _ => false
        };
    }

    private static int CompareValue(object? left, object? right)
    {
        if (left is null or DBNull)
            return right is null or DBNull ? 0 : -1;
        if (right is null or DBNull)
            return 1;
        if (decimal.TryParse(Convert.ToString(left, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var ld) &&
            decimal.TryParse(Convert.ToString(right, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var rd))
            return ld.CompareTo(rd);
        if (DateTimeOffset.TryParse(Convert.ToString(left, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var ldt) &&
            DateTimeOffset.TryParse(Convert.ToString(right, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var rdt))
            return ldt.CompareTo(rdt);
        return string.Compare(Convert.ToString(left, CultureInfo.InvariantCulture), Convert.ToString(right, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
    }
}
