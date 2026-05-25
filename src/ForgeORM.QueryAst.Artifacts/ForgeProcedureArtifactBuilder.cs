using ForgeORM.Abstractions;
using ForgeORM.QueryAst;

namespace ForgeORM.QueryAst.Artifacts;

public sealed class ForgeProcedureArtifactBuilder<T>
{
    private readonly IForgeAstSelectBuilder<T> _query;
    private readonly string _name;
    private readonly string _schema;
    private readonly List<ForgeArtifactParameter> _parameters = [];
    private string? _reason;

    /// <summary>
    /// Executes the ForgeProcedureArtifactBuilder operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="schema">The schema value.</param>
    /// <returns>The result of the ForgeProcedureArtifactBuilder operation.</returns>
    public ForgeProcedureArtifactBuilder(IForgeAstSelectBuilder<T> query, string name, string schema)
    {
        _query = query;
        _name = name;
        _schema = schema;
    }

    /// <summary>
    /// Executes the WithParameter operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="dbType">The dbType value.</param>
    /// <param name="defaultValue">The defaultValue value.</param>
    /// <returns>The result of the WithParameter operation.</returns>
    public ForgeProcedureArtifactBuilder<T> WithParameter(string name, string dbType, string? defaultValue = null)
    {
        _parameters.Add(new ForgeArtifactParameter { Name = name, DbType = dbType, DefaultValue = defaultValue });
        return this;
    }

    /// <summary>
    /// Executes the WithReason operation.
    /// </summary>
    /// <param name="reason">The reason value.</param>
    /// <returns>The result of the WithReason operation.</returns>
    public ForgeProcedureArtifactBuilder<T> WithReason(string reason) { _reason = reason; return this; }

    /// <summary>
    /// Executes the Render operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the Render operation.</returns>
    public ForgeArtifactRenderResult Render(IForgeDatabaseProvider provider)
    {
        var select = _query.Render(provider);
        var fullName = $"{_schema}.{_name}";
        var parameterSql = string.Join(",\n    ", _parameters.Select(x => x.Render()));

        var sql = provider.ProviderName switch
        {
            "SqlServer" => $"""
                CREATE OR ALTER PROCEDURE {fullName}
                    {parameterSql}
                AS
                BEGIN
                    SET NOCOUNT ON;

                    {select.Sql}
                END
                """,
            "PostgreSql" => $"""
                CREATE OR REPLACE FUNCTION {fullName}({ToPostgreSqlParameters()})
                RETURNS SETOF RECORD
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RETURN QUERY
                    {select.Sql};
                END;
                $$;
                """,
            "MySql" => $"""
                DROP PROCEDURE IF EXISTS {fullName};
                CREATE PROCEDURE {fullName}({parameterSql})
                BEGIN
                    {select.Sql};
                END
                """,
            "Oracle" => $"""
                CREATE OR REPLACE PROCEDURE {fullName}
                AS
                BEGIN
                    OPEN :ResultCursor FOR
                    {select.Sql};
                END;
                """,
            "Sqlite" => throw new NotSupportedException("SQLite does not support stored procedures."),
            _ => throw new NotSupportedException($"Stored procedure artifact is not supported for provider {provider.ProviderName}.")
        };

        var artifact = new ForgeDbArtifact
        {
            Type = ForgeDbArtifactType.StoredProcedure,
            Schema = _schema,
            Name = _name,
            SqlDefinition = sql,
            ChangeReason = _reason
        };

        return new ForgeArtifactRenderResult { Artifact = artifact, DeploymentSql = sql };
    }

    private string ToPostgreSqlParameters() => string.Join(", ", _parameters.Select(x => $"{x.Name.TrimStart('@', ':')} {x.DbType}"));
}
