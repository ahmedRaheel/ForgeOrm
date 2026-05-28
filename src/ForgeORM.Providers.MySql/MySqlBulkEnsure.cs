using System.Text;
using ForgeORM.Core;
using MySqlConnector;

namespace ForgeORM.Providers.MySql;

internal static class MySqlBulkEnsure
{
    public static async ValueTask EnsureTempTableAsync(
        MySqlConnection connection,
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
        sb.Append("CREATE TEMPORARY TABLE IF NOT EXISTS ").Append(Quote(plan.TempTableName)).Append(" (");

        for (var i = 0; i < plan.Columns.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            var c = plan.Columns[i];
            sb.Append(Quote(c.ColumnName)).Append(' ').Append(ToType(c.ClrType));
        }

        sb.Append(");");
        return sb.ToString();
    }

    private static string Quote(string value) => "`" + value.Replace("`", "``", StringComparison.Ordinal) + "`";

    private static string ToType(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        if (actual.IsEnum) return "varchar(100)";
        if (actual == typeof(string)) return "longtext";
        if (actual == typeof(int)) return "int";
        if (actual == typeof(long)) return "bigint";
        if (actual == typeof(short)) return "smallint";
        if (actual == typeof(bool)) return "bit";
        if (actual == typeof(Guid)) return "char(36)";
        if (actual == typeof(decimal)) return "decimal(38,10)";
        if (actual == typeof(double)) return "double";
        if (actual == typeof(float)) return "float";
        if (actual == typeof(DateTime)) return "datetime(6)";
        if (actual == typeof(DateTimeOffset)) return "datetime(6)";
        if (actual == typeof(TimeSpan)) return "time(6)";
        if (actual == typeof(byte[])) return "longblob";
        return "longtext";
    }
}
