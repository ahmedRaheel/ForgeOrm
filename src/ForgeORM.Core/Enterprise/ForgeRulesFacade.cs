using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed class ForgeRulesFacade
{
    private readonly ForgeDb _db;
    internal ForgeRulesFacade(ForgeDb db) => _db = db;
    public ValueTask<TResult?> EvaluateAsync<TResult>(string ruleSet, object facts, CancellationToken cancellationToken = default)
    {
        var factory = ForgeRuntimeAccessorCache.Constructor(typeof(TResult));
        return ValueTask.FromResult((TResult?)factory());
    }
}
