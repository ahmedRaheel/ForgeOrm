namespace ForgeORM.VectorSearch;

public sealed record ForgeVectorSearchResult(string Id, string Text, double Score, IReadOnlyDictionary<string, string>? Metadata = null);
