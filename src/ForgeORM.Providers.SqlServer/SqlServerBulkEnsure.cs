using System.Collections.Concurrent;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Providers.SqlServer;

internal enum SqlServerTableTypePurpose
{
    InsertOrUpdate,
    DeleteKeyOnly
}

internal enum SqlServerTableTypeMismatchBehavior
{
    Throw,
    DropAndRecreate
}

/// <summary>
/// Ensures SQL Server table type exists and matches the current Forge bulk plan.
/// SQL Server table types cannot be altered, so mismatch is either thrown or
/// drop/recreate when explicitly enabled. Verified schemas are cached by hash.
/// </summary>
internal static class SqlServerBulkEnsure
{
    private static readonly ConcurrentDictionary<string, string> Verified = new(StringComparer.OrdinalIgnoreCase);

    public static SqlServerTableTypeMismatchBehavior MismatchBehavior { get; set; } =
        SqlServerTableTypeMismatchBehavior.DropAndRecreate;

    public static async ValueTask EnsureTableTypeCompatibleAsync(
        SqlConnection connection,
        SqlServerBulkPlan plan,
        SqlServerTableTypePurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var schema = string.IsNullOrWhiteSpace(plan.SchemaName) ? "dbo" : plan.SchemaName;
        var typeName = purpose == SqlServerTableTypePurpose.DeleteKeyOnly
            ? plan.KeyTableTypeName
            : plan.TableTypeName;

        var expectedHash = BuildHash(plan, purpose);
        var cacheKey = $"{connection.DataSource}|{connection.Database}|{schema}.{typeName}|{expectedHash}";

        if (Verified.TryGetValue(cacheKey, out var cached) && cached == expectedHash)
            return;

        var actualHash = await ReadExistingHashAsync(connection, schema, typeName, cancellationToken)
            .ConfigureAwait(false);

        if (actualHash is null)
        {
            await CreateTypeAsync(connection, schema, typeName, plan, purpose, cancellationToken)
                .ConfigureAwait(false);

            Verified[cacheKey] = expectedHash;
            return;
        }

        if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            if (MismatchBehavior == SqlServerTableTypeMismatchBehavior.Throw)
            {
                throw new InvalidOperationException(
                    $"SQL Server TVP type '{schema}.{typeName}' exists but schema does not match current entity metadata.");
            }

            await DropTypeAsync(connection, schema, typeName, cancellationToken).ConfigureAwait(false);
            await CreateTypeAsync(connection, schema, typeName, plan, purpose, cancellationToken).ConfigureAwait(false);
        }

        Verified[cacheKey] = expectedHash;
    }

    private static async ValueTask<string?> ReadExistingHashAsync(
        SqlConnection connection,
        string schema,
        string typeName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.name, t.name, c.max_length, c.precision, c.scale, c.is_nullable, c.column_id
            FROM sys.table_types tt
            INNER JOIN sys.schemas s ON s.schema_id = tt.schema_id
            INNER JOIN sys.columns c ON c.object_id = tt.type_table_object_id
            INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
            WHERE s.name = @Schema AND tt.name = @TypeName
            ORDER BY c.column_id;
            """;

        command.Parameters.Add("@Schema", SqlDbType.NVarChar, 128).Value = schema;
        command.Parameters.Add("@TypeName", SqlDbType.NVarChar, 128).Value = typeName;

        var sb = new StringBuilder();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return null;

        do
        {
            sb.Append(reader.GetString(0)).Append('|')
              .Append(reader.GetString(1)).Append('|')
              .Append(reader.GetInt16(2)).Append('|')
              .Append(reader.GetByte(3)).Append('|')
              .Append(reader.GetByte(4)).Append('|')
              .Append(reader.GetBoolean(5)).Append(';');
        }
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false));

        return Sha256(sb.ToString());
    }

    private static string BuildHash(SqlServerBulkPlan plan, SqlServerTableTypePurpose purpose)
    {
        var sb = new StringBuilder();

        if (purpose == SqlServerTableTypePurpose.DeleteKeyOnly)
        {
            sb.Append(plan.KeyColumn).Append('|')
              .Append(ToSqlServerType(plan.KeyClrType, null, null, null))
              .Append("|true;");
        }
        else
        {
            for (var i = 0; i < plan.Columns.Count; i++)
            {
                var c = plan.Columns[i];
                sb.Append(c.ColumnName).Append('|')
                  .Append(ToSqlServerType(c.ClrType, c.Length, c.Precision, c.Scale))
                  .Append("|true;");
            }
        }

        return Sha256(sb.ToString());
    }

    private static async ValueTask CreateTypeAsync(
        SqlConnection connection,
        string schema,
        string typeName,
        SqlServerBulkPlan plan,
        SqlServerTableTypePurpose purpose,
        CancellationToken cancellationToken)
    {
        var sql = BuildCreateTypeSql(schema, typeName, plan, purpose);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask DropTypeAsync(
        SqlConnection connection,
        string schema,
        string typeName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP TYPE {Quote(schema)}.{Quote(typeName)};";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string BuildCreateTypeSql(
        string schema,
        string typeName,
        SqlServerBulkPlan plan,
        SqlServerTableTypePurpose purpose)
    {
        var sb = new StringBuilder();

        sb.Append("CREATE TYPE ")
          .Append(Quote(schema))
          .Append('.')
          .Append(Quote(typeName))
          .Append(" AS TABLE (");

        if (purpose == SqlServerTableTypePurpose.DeleteKeyOnly)
        {
            sb.Append(Quote(plan.KeyColumn))
              .Append(' ')
              .Append(ToSqlServerDefinition(plan.KeyClrType, null, null, null))
              .Append(" NULL");
        }
        else
        {
            for (var i = 0; i < plan.Columns.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                var c = plan.Columns[i];

                sb.Append(Quote(c.ColumnName))
                  .Append(' ')
                  .Append(ToSqlServerDefinition(c.ClrType, c.Length, c.Precision, c.Scale))
                  .Append(" NULL");
            }
        }

        sb.Append(')');
        return sb.ToString();
    }

    private static string Quote(string value)
        => "[" + value.Trim('[', ']').Replace("]", "]]", StringComparison.Ordinal) + "]";

    private static string ToSqlServerType(Type type, int? length, byte? precision, byte? scale)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;

        if (actual.IsEnum) return "nvarchar";
        if (actual == typeof(string)) return "nvarchar";
        if (actual == typeof(int)) return "int";
        if (actual == typeof(long)) return "bigint";
        if (actual == typeof(short)) return "smallint";
        if (actual == typeof(byte)) return "tinyint";
        if (actual == typeof(bool)) return "bit";
        if (actual == typeof(Guid)) return "uniqueidentifier";
        if (actual == typeof(decimal)) return "decimal";
        if (actual == typeof(double)) return "float";
        if (actual == typeof(float)) return "real";
        if (actual == typeof(DateTime)) return "datetime2";
        if (actual == typeof(DateTimeOffset)) return "datetimeoffset";
        if (actual == typeof(TimeSpan)) return "time";
        if (actual == typeof(byte[])) return "varbinary";

        return "nvarchar";
    }

    private static string ToSqlServerDefinition(Type type, int? length, byte? precision, byte? scale)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;

        if (actual.IsEnum) return "NVARCHAR(100)";
        if (actual == typeof(string)) return length is > 0 and <= 4000 ? $"NVARCHAR({length.Value})" : "NVARCHAR(MAX)";
        if (actual == typeof(int)) return "INT";
        if (actual == typeof(long)) return "BIGINT";
        if (actual == typeof(short)) return "SMALLINT";
        if (actual == typeof(byte)) return "TINYINT";
        if (actual == typeof(bool)) return "BIT";
        if (actual == typeof(Guid)) return "UNIQUEIDENTIFIER";
        if (actual == typeof(decimal)) return $"DECIMAL({precision ?? 18},{scale ?? 2})";
        if (actual == typeof(double)) return "FLOAT";
        if (actual == typeof(float)) return "REAL";
        if (actual == typeof(DateTime)) return "DATETIME2";
        if (actual == typeof(DateTimeOffset)) return "DATETIMEOFFSET";
        if (actual == typeof(TimeSpan)) return "TIME";
        if (actual == typeof(byte[])) return "VARBINARY(MAX)";

        return "NVARCHAR(MAX)";
    }

    private static string Sha256(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }
}
