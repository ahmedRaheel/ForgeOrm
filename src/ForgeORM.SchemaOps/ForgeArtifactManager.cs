using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using ForgeORM.Abstractions;
using ForgeORM.QueryAst.Artifacts;

namespace ForgeORM.SchemaOps;

public interface IForgeArtifactManager
/// <summary>
/// Defines the EnsureHistoryTableAsync operation.
/// </summary>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the EnsureHistoryTableAsync operation.</returns>
{
    /// <summary>
    /// Defines the EnsureHistoryTableAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the EnsureHistoryTableAsync operation.</returns>
    ValueTask EnsureHistoryTableAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the CreateOrUpdateAsync operation.
    /// </summary>
    /// <param name="artifact">The artifact value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the CreateOrUpdateAsync operation.</returns>
    ValueTask<ForgeArtifactApplyResult> CreateOrUpdateAsync(ForgeDbArtifact artifact, CancellationToken cancellationToken = default);
}

public sealed class ForgeArtifactApplyResult
{
    public required string ArtifactName { get; init; }
    public required string Schema { get; init; }
    public required ForgeDbArtifactType ArtifactType { get; init; }
    public required int VersionNo { get; init; }
    public required bool Applied { get; init; }
    public required bool SkippedBecauseUnchanged { get; init; }
    public string? SqlHash { get; init; }
}

internal sealed class ForgeArtifactVersion
{
    public long Id { get; init; }
    public string ArtifactType { get; init; } = "";
    public string SchemaName { get; init; } = "";
    public string ArtifactName { get; init; } = "";
    public int VersionNo { get; init; }
    public string SqlHash { get; init; } = "";
    public string SqlDefinition { get; init; } = "";
}

public sealed class ForgeArtifactManager : IForgeArtifactManager
{
    private readonly Func<DbConnection> _connectionFactory;
    private readonly IForgeDatabaseProvider _provider;

    /// <summary>
    /// Executes the ForgeArtifactManager operation.
    /// </summary>
    /// <param name="connectionFactory">The connectionFactory value.</param>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the ForgeArtifactManager operation.</returns>
    public ForgeArtifactManager(Func<DbConnection> connectionFactory, IForgeDatabaseProvider provider)
    {
        _connectionFactory = connectionFactory;
        _provider = provider;
    }

    /// <summary>
    /// Executes the EnsureHistoryTableAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the EnsureHistoryTableAsync operation.</returns>
    public async ValueTask EnsureHistoryTableAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);
        var sql = _provider.ProviderName switch
        {
            "SqlServer" => SqlServerHistoryTableSql,
            "PostgreSql" => PostgreSqlHistoryTableSql,
            "MySql" => MySqlHistoryTableSql,
            "Sqlite" => SqliteHistoryTableSql,
            _ => SqlServerHistoryTableSql
        };
        await ForgeSchemaAdo.ExecuteAsync(connection, sql, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Executes the CreateOrUpdateAsync operation.
    /// </summary>
    /// <param name="artifact">The artifact value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the CreateOrUpdateAsync operation.</returns>
    public async ValueTask<ForgeArtifactApplyResult> CreateOrUpdateAsync(ForgeDbArtifact artifact, CancellationToken cancellationToken = default)
    {
        await EnsureHistoryTableAsync(cancellationToken);
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        var hash = Sha256(artifact.SqlDefinition);

        var latest = (await ForgeSchemaAdo.QueryAsync<ForgeArtifactVersion>(connection, LatestHistorySql, new
            {
                ArtifactType = artifact.Type.ToString(),
                SchemaName = artifact.Schema,
                ArtifactName = artifact.Name
            }, cancellationToken: cancellationToken)).FirstOrDefault();

        if (latest is not null && latest.SqlHash == hash)
        {
            return new ForgeArtifactApplyResult
            {
                ArtifactName = artifact.Name,
                Schema = artifact.Schema,
                ArtifactType = artifact.Type,
                VersionNo = latest.VersionNo,
                Applied = false,
                SkippedBecauseUnchanged = true,
                SqlHash = hash
            };
        }

        var nextVersion = (latest?.VersionNo ?? 0) + 1;

        await ForgeSchemaAdo.ExecuteAsync(connection, artifact.SqlDefinition, cancellationToken: cancellationToken);

        await ForgeSchemaAdo.ExecuteAsync(connection, InsertHistorySql, new
        {
            ArtifactType = artifact.Type.ToString(),
            SchemaName = artifact.Schema,
            ArtifactName = artifact.Name,
            VersionNo = nextVersion,
            SqlHash = hash,
            SqlDefinition = artifact.SqlDefinition,
            PreviousSqlHash = latest?.SqlHash,
            PreviousSqlDefinition = latest?.SqlDefinition,
            ChangeReason = artifact.ChangeReason,
            AppliedBy = Environment.UserName,
            MachineName = Environment.MachineName,
            ApplicationName = AppDomain.CurrentDomain.FriendlyName
        }, cancellationToken: cancellationToken);

        return new ForgeArtifactApplyResult
        {
            ArtifactName = artifact.Name,
            Schema = artifact.Schema,
            ArtifactType = artifact.Type,
            VersionNo = nextVersion,
            Applied = true,
            SkippedBecauseUnchanged = false,
            SqlHash = hash
        };
    }

    private static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private const string LatestHistorySql = """
        SELECT *
        FROM ForgeOrmArtifactHistory
        WHERE ArtifactType = @ArtifactType
          AND SchemaName = @SchemaName
          AND ArtifactName = @ArtifactName
        ORDER BY VersionNo DESC
        """;

    private const string InsertHistorySql = """
        INSERT INTO ForgeOrmArtifactHistory
        (
            ArtifactType, SchemaName, ArtifactName, VersionNo, SqlHash, SqlDefinition,
            PreviousSqlHash, PreviousSqlDefinition, ChangeReason, AppliedBy, MachineName, ApplicationName
        )
        VALUES
        (
            @ArtifactType, @SchemaName, @ArtifactName, @VersionNo, @SqlHash, @SqlDefinition,
            @PreviousSqlHash, @PreviousSqlDefinition, @ChangeReason, @AppliedBy, @MachineName, @ApplicationName
        )
        """;

    private const string SqlServerHistoryTableSql = """
        IF OBJECT_ID('dbo.ForgeOrmArtifactHistory', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ForgeOrmArtifactHistory
            (
                Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                ArtifactType NVARCHAR(50) NOT NULL,
                SchemaName NVARCHAR(128) NOT NULL,
                ArtifactName NVARCHAR(256) NOT NULL,
                VersionNo INT NOT NULL,
                SqlHash NVARCHAR(128) NOT NULL,
                SqlDefinition NVARCHAR(MAX) NOT NULL,
                PreviousSqlHash NVARCHAR(128) NULL,
                PreviousSqlDefinition NVARCHAR(MAX) NULL,
                ChangeReason NVARCHAR(1000) NULL,
                AppliedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                AppliedBy NVARCHAR(256) NULL,
                MachineName NVARCHAR(256) NULL,
                ApplicationName NVARCHAR(256) NULL
            );
        END
        """;

    private const string PostgreSqlHistoryTableSql = """
        CREATE TABLE IF NOT EXISTS ForgeOrmArtifactHistory
        (
            Id BIGSERIAL PRIMARY KEY,
            ArtifactType VARCHAR(50) NOT NULL,
            SchemaName VARCHAR(128) NOT NULL,
            ArtifactName VARCHAR(256) NOT NULL,
            VersionNo INT NOT NULL,
            SqlHash VARCHAR(128) NOT NULL,
            SqlDefinition TEXT NOT NULL,
            PreviousSqlHash VARCHAR(128) NULL,
            PreviousSqlDefinition TEXT NULL,
            ChangeReason VARCHAR(1000) NULL,
            AppliedAtUtc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            AppliedBy VARCHAR(256) NULL,
            MachineName VARCHAR(256) NULL,
            ApplicationName VARCHAR(256) NULL
        );
        """;

    private const string MySqlHistoryTableSql = """
        CREATE TABLE IF NOT EXISTS ForgeOrmArtifactHistory
        (
            Id BIGINT AUTO_INCREMENT PRIMARY KEY,
            ArtifactType VARCHAR(50) NOT NULL,
            SchemaName VARCHAR(128) NOT NULL,
            ArtifactName VARCHAR(256) NOT NULL,
            VersionNo INT NOT NULL,
            SqlHash VARCHAR(128) NOT NULL,
            SqlDefinition LONGTEXT NOT NULL,
            PreviousSqlHash VARCHAR(128) NULL,
            PreviousSqlDefinition LONGTEXT NULL,
            ChangeReason VARCHAR(1000) NULL,
            AppliedAtUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            AppliedBy VARCHAR(256) NULL,
            MachineName VARCHAR(256) NULL,
            ApplicationName VARCHAR(256) NULL
        );
        """;

    private const string SqliteHistoryTableSql = """
        CREATE TABLE IF NOT EXISTS ForgeOrmArtifactHistory
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ArtifactType TEXT NOT NULL,
            SchemaName TEXT NOT NULL,
            ArtifactName TEXT NOT NULL,
            VersionNo INTEGER NOT NULL,
            SqlHash TEXT NOT NULL,
            SqlDefinition TEXT NOT NULL,
            PreviousSqlHash TEXT NULL,
            PreviousSqlDefinition TEXT NULL,
            ChangeReason TEXT NULL,
            AppliedAtUtc TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
            AppliedBy TEXT NULL,
            MachineName TEXT NULL,
            ApplicationName TEXT NULL
        );
        """;
}


internal static class ForgeSchemaAdo
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public static async ValueTask<IReadOnlyList<T>> QueryAsync<T>(DbConnection connection, string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection, sql, parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<T>();
        while (await reader.ReadAsync(cancellationToken)) rows.Add(Map<T>(reader));
        return rows;
    }

    /// <summary>
    /// Executes the ExecuteAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExecuteAsync operation.</returns>
    public static async ValueTask<int> ExecuteAsync(DbConnection connection, string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection, sql, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DbCommand CreateCommand(DbConnection connection, string sql, object? parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        if (parameters is not null)
        {
            foreach (var prop in parameters.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(p => p.CanRead))
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@" + prop.Name;
                parameter.Value = prop.GetValue(parameters) ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }
        return command;
    }

    private static T Map<T>(DbDataReader reader)
    {
        var instance = Activator.CreateInstance<T>();
        var props = typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(p => p.CanWrite).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (!props.TryGetValue(reader.GetName(i), out var prop) || reader.IsDBNull(i)) continue;
            var value = reader.GetValue(i);
            var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            prop.SetValue(instance, Convert.ChangeType(value, type));
        }
        return instance;
    }
}
