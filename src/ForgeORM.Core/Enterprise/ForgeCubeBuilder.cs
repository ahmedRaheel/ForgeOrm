using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed class ForgeCubeBuilder<T>
{
    private readonly ForgeDb _db;
    private readonly List<string> _dimensions = [];
    private readonly List<ForgeCubeMeasure> _measures = [];
    internal ForgeCubeBuilder(ForgeDb db) => _db = db;

    public ForgeCubeBuilder<T> Dimension<TValue>(Expression<Func<T, TValue>> selector)
    {
        _dimensions.Add(ForgeExpressionTranslator.MemberName(selector));
        return this;
    }

    public ForgeCubeBuilder<T> Measure(string name, Func<ForgeCubeMeasureBuilder<T>, ForgeCubeMeasure> configure)
    {
        _measures.Add(configure(new ForgeCubeMeasureBuilder<T>(name)));
        return this;
    }

    public async ValueTask<IReadOnlyList<Dictionary<string, object?>>> BuildAsync(CancellationToken cancellationToken = default)
    {
        var dimensions = _dimensions.Count == 0 ? ["1 AS Bucket"] : _dimensions.ToArray();
        var measures = _measures.Count == 0 ? ["COUNT(1) AS Count"] : _measures.Select(m => m.Sql).ToArray();
        var groupBy = _dimensions.Count == 0 ? string.Empty : " GROUP BY " + string.Join(", ", _dimensions);
        var sql = $"SELECT {string.Join(", ", dimensions.Concat(measures))} FROM {typeof(T).Name}{groupBy}";
        return await _db.QueryDictionaryAsync(sql, parameters: null, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
