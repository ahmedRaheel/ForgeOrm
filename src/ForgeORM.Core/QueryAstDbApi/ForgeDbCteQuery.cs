using System.Linq.Expressions;
using System.Text;

namespace ForgeORM.Core;

public sealed class ForgeDbCteQuery<T>
{
    private readonly ForgeDb _db;
    private string? _cteName;
    private string? _cteSql;
    private string? _from;

    internal ForgeDbCteQuery(ForgeDb db) => _db = db;

    public ForgeDbCteQuery<T> With(string name, Func<ForgeDbCteInnerQuery<T>, ForgeDbCteInnerQuery<T>> build)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("CTE name is required.", nameof(name));
        if (build is null) throw new ArgumentNullException(nameof(build));
        _cteName = name;
        _cteSql = build(new ForgeDbCteInnerQuery<T>()).ToSql();
        return this;
    }

    public ForgeDbCteQuery<T> From(string source)
    {
        _from = source;
        return this;
    }

    public string ToSql()
    {
        if (string.IsNullOrWhiteSpace(_cteName) || string.IsNullOrWhiteSpace(_cteSql))
            throw new InvalidOperationException("CTE query requires With(name, query).");
        var from = string.IsNullOrWhiteSpace(_from) ? _cteName : _from;
        return $"WITH {_cteName} AS ({_cteSql}) SELECT * FROM {from}";
    }

    public ValueTask<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default)
        => _db.QueryAsync<T>(ToSql(), cancellationToken);
}
