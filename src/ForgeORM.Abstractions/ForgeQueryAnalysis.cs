using System.Data;

namespace ForgeORM.Abstractions;

public sealed class ForgeQueryAnalysis
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public List<string> Suggestions { get; init; } = [];
}
