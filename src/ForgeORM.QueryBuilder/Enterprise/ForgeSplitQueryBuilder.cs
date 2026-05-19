using System.Linq.Expressions;

namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Builds split-query SQL to fetch parents first and children in separate queries.
/// </summary>
public sealed class ForgeSplitQueryBuilder<TParent>
{
    private readonly ForgeEnterpriseQuery<TParent> _parent = new();
    private readonly List<ForgeSplitInclude> _includes = [];

    /// <summary>
    /// Sets the parent table.
    /// </summary>
    public ForgeSplitQueryBuilder<TParent> From(string table)
    {
        _parent.From(table);
        return this;
    }

    /// <summary>
    /// Adds a parent filter.
    /// </summary>
    public ForgeSplitQueryBuilder<TParent> Where(Expression<Func<TParent, bool>> predicate)
    {
        _parent.Where(predicate);
        return this;
    }

    /// <summary>
    /// Adds parent ordering.
    /// </summary>
    public ForgeSplitQueryBuilder<TParent> OrderBy(Expression<Func<TParent, object?>> column, ForgeSortDirection direction = ForgeSortDirection.Ascending)
    {
        _parent.OrderBy(column, direction);
        return this;
    }

    /// <summary>
    /// Adds parent paging.
    /// </summary>
    public ForgeSplitQueryBuilder<TParent> Page(int page, int pageSize)
    {
        _parent.Page(page, pageSize);
        return this;
    }

    /// <summary>
    /// Includes a child collection by explicit mapping.
    /// </summary>
    public ForgeSplitQueryBuilder<TParent> Include(string childProperty, string childTable, string parentKey, string childForeignKey)
    {
        _includes.Add(new ForgeSplitInclude
        {
            ChildProperty = childProperty,
            ChildTable = childTable,
            ParentKey = parentKey,
            ChildForeignKey = childForeignKey
        });
        return this;
    }

    /// <summary>
    /// Creates split-query SQL statements.
    /// </summary>
    public ForgeSplitQueryPlan ToPlan(ForgeQueryProviderDialect dialect = ForgeQueryProviderDialect.SqlServer)
    {
        var parentQuery = _parent.ToSql(dialect);
        var childQueries = _includes.Select(x => new ForgeSqlQuery
        {
            Sql = $"SELECT * FROM {x.ChildTable} WHERE {x.ChildForeignKey} IN @ParentIds",
            Parameters = [new ForgeSqlParameter("@ParentIds", null)]
        }).ToArray();

        return new ForgeSplitQueryPlan
        {
            ParentQuery = parentQuery,
            ChildQueries = childQueries
        };
    }
}
