using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ForgeORM.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace ForgeORM.NextGen;

public sealed class ForgeTransparentCommand
{
    public required string Sql { get; init; }
    public object? Parameters { get; init; }
    public string DebugView => Parameters is null ? Sql : $"{Sql}\n-- params: {JsonSerializer.Serialize(Parameters)}";

    /// <summary>
    /// Executes the Execute operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the Execute operation.</returns>
    public int Execute(IForgeDb db) => db.Execute(Sql, Parameters);
    /// <summary>
    /// Executes the ExecuteAsync operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExecuteAsync operation.</returns>
    public ValueTask<int> ExecuteAsync(IForgeDb db, CancellationToken cancellationToken = default)
        => db.ExecuteAsync(Sql, Parameters, cancellationToken: cancellationToken);
}
