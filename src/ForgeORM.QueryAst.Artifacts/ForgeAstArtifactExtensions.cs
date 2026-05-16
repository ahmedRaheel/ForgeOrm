using ForgeORM.Abstractions;
using ForgeORM.QueryAst;

namespace ForgeORM.QueryAst.Artifacts;

public enum ForgeDbArtifactType
{
    View,
    StoredProcedure,
    Function,
    Script
}

public sealed class ForgeDbArtifact
{
    public required ForgeDbArtifactType Type { get; init; }
    public required string Schema { get; init; }
    public required string Name { get; init; }
    public required string SqlDefinition { get; init; }
    public string? ChangeReason { get; init; }
    /// <summary>
    /// Executes the string.IsNullOrWhiteSpace operation.
    /// </summary>
    /// <param name="Schema">The Schema value.</param>
    /// <returns>The operation result.</returns>
    public string FullName => string.IsNullOrWhiteSpace(Schema) ? Name : $"{Schema}.{Name}";
}

public sealed class ForgeArtifactRenderResult
{
    public required ForgeDbArtifact Artifact { get; init; }
    public required string DeploymentSql { get; init; }
}

public sealed class ForgeArtifactParameter
{
    public required string Name { get; init; }
    public required string DbType { get; init; }
    public string? DefaultValue { get; init; }
    /// <summary>
    /// Initializes or executes the Render operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public string Render() => DefaultValue is null ? $"{Name} {DbType}" : $"{Name} {DbType} = {DefaultValue}";
}

public static class ForgeAstArtifactExtensions
{
    /// <summary>
    /// Initializes or executes the AsView operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="schema">The schema value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeViewArtifactBuilder<T> AsView<T>(this IForgeAstSelectBuilder<T> query, string name, string schema = "dbo")
        => new(query, name, schema);

    /// <summary>
    /// Initializes or executes the AsProcedure operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="schema">The schema value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeProcedureArtifactBuilder<T> AsProcedure<T>(this IForgeAstSelectBuilder<T> query, string name, string schema = "dbo")
        => new(query, name, schema);
}

public sealed class ForgeViewArtifactBuilder<T>
{
    private readonly IForgeAstSelectBuilder<T> _query;
    private readonly string _name;
    private readonly string _schema;
    private string? _reason;

    /// <summary>
    /// Initializes or executes the ForgeViewArtifactBuilder operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="schema">The schema value.</param>
    public ForgeViewArtifactBuilder(IForgeAstSelectBuilder<T> query, string name, string schema)
    {
        _query = query;
        _name = name;
        _schema = schema;
    }

    /// <summary>
    /// Initializes or executes the WithReason operation.
    /// </summary>
    /// <param name="reason">The reason value.</param>
    /// <returns>The operation result.</returns>
    public ForgeViewArtifactBuilder<T> WithReason(string reason) { _reason = reason; return this; }

    /// <summary>
    /// Initializes or executes the Render operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The operation result.</returns>
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

public sealed class ForgeProcedureArtifactBuilder<T>
{
    private readonly IForgeAstSelectBuilder<T> _query;
    private readonly string _name;
    private readonly string _schema;
    private readonly List<ForgeArtifactParameter> _parameters = [];
    private string? _reason;

    /// <summary>
    /// Initializes or executes the ForgeProcedureArtifactBuilder operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="schema">The schema value.</param>
    public ForgeProcedureArtifactBuilder(IForgeAstSelectBuilder<T> query, string name, string schema)
    {
        _query = query;
        _name = name;
        _schema = schema;
    }

    /// <summary>
    /// Initializes or executes the WithParameter operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="dbType">The dbType value.</param>
    /// <param name="defaultValue">The defaultValue value.</param>
    /// <returns>The operation result.</returns>
    public ForgeProcedureArtifactBuilder<T> WithParameter(string name, string dbType, string? defaultValue = null)
    {
        _parameters.Add(new ForgeArtifactParameter { Name = name, DbType = dbType, DefaultValue = defaultValue });
        return this;
    }

    /// <summary>
    /// Initializes or executes the WithReason operation.
    /// </summary>
    /// <param name="reason">The reason value.</param>
    /// <returns>The operation result.</returns>
    public ForgeProcedureArtifactBuilder<T> WithReason(string reason) { _reason = reason; return this; }

    /// <summary>
    /// Initializes or executes the Render operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The operation result.</returns>
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
