using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.Rag;

public sealed record ForgeRagChunk(
    string Id,
    string DocumentId,
    string Text,
    int Index,
    IReadOnlyDictionary<string, string>? Metadata = null);
