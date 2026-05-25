using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using ForgeORM.Abstractions;
using ForgeORM.QueryAst.Artifacts;

namespace ForgeORM.SchemaOps;

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
