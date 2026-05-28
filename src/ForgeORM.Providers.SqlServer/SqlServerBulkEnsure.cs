using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace ForgeORM.Providers.SqlServer;

internal enum SqlServerTableTypePurpose { InsertOrUpdate, DeleteKeyOnly }

internal static class SqlServerBulkEnsure
{
    private static readonly ConcurrentDictionary<string, string> Verified = new(StringComparer.OrdinalIgnoreCase);

    public static async ValueTask EnsureTableTypeCompatibleAsync(SqlConnection connection, SqlServerBulkPlan plan, SqlServerTableTypePurpose purpose, CancellationToken cancellationToken = default)
    {
        var typeName = purpose == SqlServerTableTypePurpose.DeleteKeyOnly ? plan.KeyTvpTypeName : plan.TvpTypeName;
        var hash = BuildHash(plan, purpose);
        var cacheKey = $"{connection.DataSource}|{connection.Database}|dbo.{typeName}|{hash}";
        if (Verified.TryGetValue(cacheKey, out var cached) && cached == hash) return;

        var existing = await ReadExistingHashAsync(connection, typeName, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            await CreateTypeAsync(connection, typeName, plan, purpose, cancellationToken).ConfigureAwait(false);
        }
        else if (!string.Equals(existing, hash, StringComparison.OrdinalIgnoreCase))
        {
            await DropTypeAsync(connection, typeName, cancellationToken).ConfigureAwait(false);
            await CreateTypeAsync(connection, typeName, plan, purpose, cancellationToken).ConfigureAwait(false);
        }
        Verified[cacheKey] = hash;
    }

    private static async ValueTask<string?> ReadExistingHashAsync(SqlConnection connection, string typeName, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
        SELECT c.name, t.name, c.max_length, c.precision, c.scale, c.is_nullable
        FROM sys.table_types tt
        INNER JOIN sys.schemas s ON s.schema_id = tt.schema_id
        INNER JOIN sys.columns c ON c.object_id = tt.type_table_object_id
        INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
        WHERE s.name = N'dbo' AND tt.name = @TypeName
        ORDER BY c.column_id
        """;
        cmd.Parameters.Add("@TypeName", SqlDbType.NVarChar, 128).Value = typeName;
        var sb = new StringBuilder();
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await r.ReadAsync(ct).ConfigureAwait(false)) return null;
        do
        {
            sb.Append(r.GetString(0)).Append('|').Append(r.GetString(1)).Append('|')
              .Append(r.GetInt16(2)).Append('|').Append(r.GetByte(3)).Append('|')
              .Append(r.GetByte(4)).Append('|').Append(r.GetBoolean(5)).Append(';');
        } while (await r.ReadAsync(ct).ConfigureAwait(false));
        return Sha(sb.ToString());
    }

    private static string BuildHash(SqlServerBulkPlan plan, SqlServerTableTypePurpose purpose)
    {
        var sb = new StringBuilder();
        if (purpose == SqlServerTableTypePurpose.DeleteKeyOnly)
            sb.Append(plan.KeyColumn).Append('|').Append(ToSqlTypeName(plan.KeyType)).Append("|-1|0|0|true;");
        else
            foreach (var c in plan.Columns)
                sb.Append(c.Name).Append('|').Append(ToSqlTypeName(c.Type)).Append('|').Append(SqlMaxLength(c.Type)).Append('|').Append(SqlPrecision(c.Type)).Append('|').Append(SqlScale(c.Type)).Append("|true;");
        return Sha(sb.ToString());
    }

    private static async ValueTask CreateTypeAsync(SqlConnection connection, string typeName, SqlServerBulkPlan plan, SqlServerTableTypePurpose purpose, CancellationToken ct)
    {
        var sb = new StringBuilder().Append("CREATE TYPE [dbo].[").Append(typeName.Replace("]", "]]", StringComparison.Ordinal)).Append("] AS TABLE (");
        if (purpose == SqlServerTableTypePurpose.DeleteKeyOnly)
        {
            sb.Append('[').Append(plan.KeyColumn).Append("] ").Append(ToSqlDefinition(plan.KeyType)).Append(" NULL");
        }
        else
        {
            for (var i = 0; i < plan.Columns.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('[').Append(plan.Columns[i].Name).Append("] ").Append(ToSqlDefinition(plan.Columns[i].Type)).Append(" NULL");
            }
        }
        sb.Append(')');
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sb.ToString();
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async ValueTask DropTypeAsync(SqlConnection connection, string typeName, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DROP TYPE [dbo].[{typeName.Replace("]", "]]", StringComparison.Ordinal)}]";
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    internal static string ToSqlDefinition(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        if (t.IsEnum) return "NVARCHAR(100)";
        if (t == typeof(string)) return "NVARCHAR(MAX)";
        if (t == typeof(int)) return "INT";
        if (t == typeof(long)) return "BIGINT";
        if (t == typeof(short)) return "SMALLINT";
        if (t == typeof(byte)) return "TINYINT";
        if (t == typeof(bool)) return "BIT";
        if (t == typeof(Guid)) return "UNIQUEIDENTIFIER";
        if (t == typeof(decimal)) return "DECIMAL(18,2)";
        if (t == typeof(double)) return "FLOAT";
        if (t == typeof(float)) return "REAL";
        if (t == typeof(DateTime)) return "DATETIME2";
        if (t == typeof(DateTimeOffset)) return "DATETIMEOFFSET";
        if (t == typeof(TimeSpan)) return "TIME";
        if (t == typeof(byte[])) return "VARBINARY(MAX)";
        return "NVARCHAR(MAX)";
    }

    private static string ToSqlTypeName(Type type) => ToSqlDefinition(type).Split('(')[0].ToLowerInvariant();
    private static short SqlMaxLength(Type type) { var t = Nullable.GetUnderlyingType(type) ?? type; if (t == typeof(string) || t.IsEnum || t == typeof(byte[])) return -1; if (t == typeof(long) || t == typeof(double) || t == typeof(DateTime) || t == typeof(DateTimeOffset)) return 8; if (t == typeof(int) || t == typeof(float)) return 4; if (t == typeof(short)) return 2; if (t == typeof(byte) || t == typeof(bool)) return 1; if (t == typeof(Guid)) return 16; if (t == typeof(decimal)) return 9; return -1; }
    private static byte SqlPrecision(Type type) => (Nullable.GetUnderlyingType(type) ?? type) == typeof(decimal) ? (byte)18 : (byte)0;
    private static byte SqlScale(Type type) => (Nullable.GetUnderlyingType(type) ?? type) == typeof(decimal) ? (byte)2 : (byte)0;
    private static string Sha(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
}
