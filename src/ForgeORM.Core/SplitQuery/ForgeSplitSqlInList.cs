namespace ForgeORM.Core.SplitQuery;

internal static class ForgeSplitSqlInList
{
    public static string ExpandIds<TKey>(string sql, IReadOnlyCollection<TKey> ids)
        where TKey : notnull
    {
        if (ids.Count == 0)
            return sql.Replace("IN @Ids", "IN (NULL)", StringComparison.OrdinalIgnoreCase)
                      .Replace("IN (@Ids)", "IN (NULL)", StringComparison.OrdinalIgnoreCase);

        var list = string.Join(", ", ids.Select(FormatLiteral));
        return sql.Replace("IN @Ids", "IN (" + list + ")", StringComparison.OrdinalIgnoreCase)
                  .Replace("IN (@Ids)", "IN (" + list + ")", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatLiteral<TKey>(TKey value)
    {
        if (value is null)
            return "NULL";

        return value switch
        {
            string text => "'" + text.Replace("'", "''") + "'",
            Guid guid => "'" + guid.ToString("D") + "'",
            DateTime dateTime => "'" + dateTime.ToString("O") + "'",
            DateTimeOffset dateTimeOffset => "'" + dateTimeOffset.ToString("O") + "'",
            Enum enumValue => Convert.ToInt64(enumValue).ToString(System.Globalization.CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => "'" + value.ToString()!.Replace("'", "''") + "'"
        };
    }
}
