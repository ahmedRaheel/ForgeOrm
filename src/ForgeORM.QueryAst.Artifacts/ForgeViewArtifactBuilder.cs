using ForgeORM.Abstractions;
using ForgeORM.QueryAst;

namespace ForgeORM.QueryAst.Artifacts;

public sealed class ForgeViewArtifactBuilder<T>
{
    private readonly IForgeAstSelectBuilder<T> _query;
    private readonly string _name;
    private readonly string _schema;
    private string? _reason;

    /// <summary>
    /// Executes the ForgeViewArtifactBuilder operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="schema">The schema value.</param>
    /// <returns>The result of the ForgeViewArtifactBuilder operation.</returns>
    public ForgeViewArtifactBuilder(IForgeAstSelectBuilder<T> query, string name, string schema)
    {
        _query = query;
        _name = name;
        _schema = schema;
    }

    /// <summary>
    /// Executes the WithReason operation.
    /// </summary>
    /// <param name="reason">The reason value.</param>
    /// <returns>The result of the WithReason operation.</returns>
    public ForgeViewArtifactBuilder<T> WithReason(string reason) { _reason = reason; return this; }

    /// <summary>
    /// Executes the Render operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the Render operation.</returns>
    public ForgeArtifactRenderResult Render(IForgeDatabaseProvider provider)
    {
        var select = _query.Render(provider);
        var fullName = $"{_schema}.{_name}";

        var sql = provider.ProviderName switch
        {
            "SqlServer" => $"""
                CREATE OR ALTER VIEW {fullName}
                AS
                {select.Sql}
                """,
            "PostgreSql" => $"""
                CREATE OR REPLACE VIEW {fullName}
                AS
                {select.Sql}
                """,
            "MySql" => $"""
                CREATE OR REPLACE VIEW {fullName}
                AS
                {select.Sql}
                """,
            "Oracle" => $"""
                CREATE OR REPLACE VIEW {fullName}
                AS
                {select.Sql}
                """,
            "Sqlite" => $"""
                DROP VIEW IF EXISTS {_name};
                CREATE VIEW {_name}
                AS
                {select.Sql}
                """,
            _ => $"""
                CREATE OR REPLACE VIEW {fullName}
                AS
                {select.Sql}
                """
        };

        var artifact = new ForgeDbArtifact
        {
            Type = ForgeDbArtifactType.View,
            Schema = _schema,
            Name = _name,
            SqlDefinition = sql,
            ChangeReason = _reason
        };

        return new ForgeArtifactRenderResult { Artifact = artifact, DeploymentSql = sql };
    }
}
