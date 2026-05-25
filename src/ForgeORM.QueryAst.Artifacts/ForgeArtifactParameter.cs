using ForgeORM.Abstractions;
using ForgeORM.QueryAst;

namespace ForgeORM.QueryAst.Artifacts;

public sealed class ForgeArtifactParameter
{
    /// <summary>
    /// Executes the Render operation.
    /// </summary>
    /// <returns>The result of the Render operation.</returns>
    public required string Name { get; init; }
    /// <summary>
    /// Executes the Render operation.
    /// </summary>
    /// <returns>The result of the Render operation.</returns>
    public required string DbType { get; init; }
    /// <summary>
    /// Executes the Render operation.
    /// </summary>
    /// <returns>The result of the Render operation.</returns>
    public string? DefaultValue { get; init; }
    /// <summary>
    /// Executes the Render operation.
    /// </summary>
    /// <returns>The result of the Render operation.</returns>
    public string Render() => DefaultValue is null ? $"{Name} {DbType}" : $"{Name} {DbType} = {DefaultValue}";
}
