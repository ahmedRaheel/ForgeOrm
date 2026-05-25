using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.Rag;

public sealed record ForgeRagDocument(
    string Id,
    string Title,
    string Content,
    IReadOnlyDictionary<string, string>? Metadata = null);
