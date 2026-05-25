using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ForgeORM.Core;

namespace ForgeORM.Querying.Search;

/// <summary>
/// Stored-procedure-backed search builder.
/// </summary>
public sealed class ForgeProcedureSearch<T>
{
    private readonly ForgeDb _db;
    private readonly string _procedureName;
    private readonly Dictionary<string, object?> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private int? _page;
    private int? _pageSize;

    public ForgeProcedureSearch(
        ForgeDb db,
        string procedureName)
    {
        _db = db;
        _procedureName = procedureName;
    }

    public ForgeProcedureSearch<T> With(
        string name,
        object? value)
    {
        _parameters[Normalize(name)] = value;
        return this;
    }

    public ForgeProcedureSearch<T> WithOptional(
        string name,
        object? value)
    {
        if (value is not null)
        {
            With(name, value);
        }

        return this;
    }

    public ForgeProcedureSearch<T> Page(
        int page,
        int pageSize)
    {
        _page = Math.Max(page, 1);
        _pageSize = Math.Max(pageSize, 1);
        _parameters["Page"] = _page.Value;
        _parameters["PageSize"] = _pageSize.Value;
        return this;
    }

    public ValueTask<IReadOnlyList<T>> ToListAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.QueryProcedureAsync<T>(
            _procedureName,
            _parameters,
            cancellationToken: cancellationToken);
    }

    public async ValueTask<ForgePagedResult<T>> ToPagedAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await ToListAsync(cancellationToken);

        return new ForgePagedResult<T>
        {
            Items = items,
            Page = _page ?? 1,
            PageSize = _pageSize ?? items.Count,
            TotalRecords = items.Count
        };
    }

    private static string Normalize(string name)
    {
        return name.TrimStart('@', ':');
    }
}
