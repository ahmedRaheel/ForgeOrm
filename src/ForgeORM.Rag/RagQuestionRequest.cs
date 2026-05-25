using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.Rag;

public sealed record RagQuestionRequest
{
    public required string Question { get; init; }

    public int TopK { get; init; } = 5;

    public string? TenantId { get; init; }

    public IReadOnlyDictionary<string, string>? Filters { get; init; }

    public bool IncludeSources { get; init; } = true;

    public bool UseSemanticRanking { get; init; } = true;

    public double? MinimumScore { get; init; }

    public CancellationToken CancellationToken { get; init; }
}
