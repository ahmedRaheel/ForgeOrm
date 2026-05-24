using Microsoft.Extensions.DependencyInjection;
using ForgeORM.Abstractions;
using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.VectorSearch;

public sealed record ForgeVectorDocument(
    string Id,
    float[] Vector,
    string Text,
    IReadOnlyDictionary<string, string> Metadata);
