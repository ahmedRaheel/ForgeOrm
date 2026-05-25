using System.Linq.Expressions;
using ForgeORM.Core.Materialization;

namespace ForgeORM.Core.EntryStyles;

/// <summary>
/// Raw SQL entry object. This gives raw SQL the same terminal methods as query builders.
/// </summary>
public sealed class ForgeRawSqlQuery
{
    private readonly ForgeDb _db;
    private readonly string _sql;
    private readonly object? _parameters;
    private string? _name;

    internal ForgeRawSqlQuery(
        ForgeDb db,
        string sql,
        object? parameters = null)
    {
        _db = db;
        _sql = sql;
        _parameters = parameters;
    }

    public ForgeRawSqlQuery Named(string name)
    {
        _name = name;
        return this;
    }

    public string ToSql() => _sql;

    public ValueTask<IReadOnlyList<T>> ToListAsync<T>(
        CancellationToken cancellationToken = default)
    {
        return _db.QueryAsync<T>(
            _sql,
            _parameters,
            cancellationToken: cancellationToken);            
    }

    public ValueTask<IReadOnlyList<Dictionary<string, object?>>> ToDictionaryAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.QueryDictionaryAsync(
            _sql,
            _parameters,
            cancellationToken: cancellationToken);
    }

    public ValueTask<ForgeJsonProjection> ToJsonAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.QueryJsonProjectionAsync(
            _sql,
            _parameters,
            _name,
            cancellationToken);
    }

    public ValueTask<ForgeTabularResult> ToDataFrameAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.QueryDataFrameAsync(
            _sql,
            _parameters,
            _name,
            cancellationToken);
    }

    public ValueTask<string> ToCsvAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.QueryCsvAsync(
            _sql,
            _parameters,
            cancellationToken);
    }
}
