
using System.Runtime.CompilerServices;
using System.Text;
using ForgeORM.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ForgeORM.NextGen;

public sealed class ForgeTraceLink
{
    public required string TraceId { get; init; }
    public required string LocalUrl { get; init; }
    public required string Sql { get; init; }
    public object? Parameters { get; init; }
    public string ProviderName { get; init; } = "Unknown";
    public IReadOnlyList<string> HotPaths { get; init; } = [];
}
