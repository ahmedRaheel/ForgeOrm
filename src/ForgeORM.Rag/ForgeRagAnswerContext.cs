using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.Rag;

public sealed record ForgeRagAnswerContext(
    string Question,
    IReadOnlyList<ForgeRagChunk> Chunks,
    string Prompt);
