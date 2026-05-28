using System.Text;
using ForgeORM.Core;
using Npgsql;

namespace ForgeORM.Providers.PostgreSql;

internal static class PostgreSqlBulkEnsure
{
    public static async ValueTask EnsureTempTableAsync(
        NpgsqlConnection connection,
        ForgeBulkPlan plan,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = BuildCreateTempTableSql(plan);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string BuildCreateTempTableSql(ForgeBulkPlan plan)
    {
        var sb = new StringBuilder();
        sb.Append("CREATE TEMP TABLE IF NOT EXISTS ").Append(Quote(plan.TempTableName)).Append(" (");

        for (var i = 0; i < plan.Columns.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            var c = plan.Columns[i];
            sb.Append(Quote(c.ColumnName)).Append(' ').Append(ToType(c.ClrType));
        }

        sb.Append(") ON COMMIT DROP;");
        return sb.ToString();
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string ToType(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        if (actual.IsEnum) return "text";
        if (actual == typeof(string)) return "text";
        if (actual == typeof(int)) return "integer";
        if (actual == typeof(long)) return "bigint";
        if (actual == typeof(short)) return "smallint";
        if (actual == typeof(bool)) return "boolean";
        if (actual == typeof(Guid)) return "uuid";
        if (actual == typeof(decimal)) return "numeric";
        if (actual == typeof(double)) return "double precision";
        if (actual == typeof(float)) return "real";
        if (actual == typeof(DateTime)) return "timestamp";
        if (actual == typeof(DateTimeOffset)) return "timestamptz";
        if (actual == typeof(TimeSpan)) return "interval";
        if (actual == typeof(byte[])) return "bytea";
        return "text";
    }
}
