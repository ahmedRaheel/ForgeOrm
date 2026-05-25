using System.Data;

namespace ForgeORM.Abstractions;

public sealed class ForgeBuiltQuery
{
    public required string Sql { get; init; }
    public object? Parameters { get; init; }
}
