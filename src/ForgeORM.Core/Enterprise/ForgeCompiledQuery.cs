using System.Linq.Expressions;

namespace ForgeORM.Core;

/// <summary>
/// Fluent compiled query registration/execution foundation.
/// </summary>
public sealed class ForgeCompiledQuery<TEntity>
    where TEntity : class, new()
{
    private readonly ForgeDb _db;
    private readonly string _name;
    private readonly ForgeQueryBuilder<TEntity> _builder;

    internal ForgeCompiledQuery(ForgeDb db, string name)
    {
        _db = db;
        _name = name;
        _builder = db.Query<TEntity>().Tag(name);
    }

    public ForgeCompiledQuery<TEntity> Configure(Action<ForgeQueryBuilder<TEntity>> configure)
    {
        configure(_builder);
        return this;
    }

    public ValueTask<IReadOnlyList<TEntity>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _builder.Profile(_name).CacheFor(TimeSpan.FromMinutes(10)).ToListAsync(cancellationToken);
    }

    public string ToSql() => _builder.ToSql();
}
