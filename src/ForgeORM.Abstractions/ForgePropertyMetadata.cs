using System.Data;

namespace ForgeORM.Abstractions;

public sealed class ForgePropertyMetadata
{
    public required string PropertyName { get; init; }
    public required string ColumnName { get; init; }
    public required Type PropertyType { get; init; }
    public bool IsKey { get; init; }
    public bool IsCode { get; init; }
    public bool IsComputed { get; init; }
}
