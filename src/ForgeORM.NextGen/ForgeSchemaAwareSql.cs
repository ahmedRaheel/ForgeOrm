
using System.Runtime.CompilerServices;
using System.Text;
using ForgeORM.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ForgeORM.NextGen;

public sealed class ForgeSchemaAwareSql
{
    public required string Sql { get; init; }
    public required Dictionary<string, object?> Parameters { get; init; }
    public IReadOnlyList<string> ReferencedTables { get; init; } = [];
    public IReadOnlyList<string> ReferencedColumns { get; init; } = [];
}
