using System.Linq.Expressions;
using System.Text;

namespace ForgeORM.Core;

public sealed class ForgeDbCteInnerQuery<T>
{
    private string _table = typeof(T).Name;
    private readonly List<string> _where = new();

    public ForgeDbCteInnerQuery<T> From<TEntity>()
    {
        _table = typeof(TEntity).Name;
        return this;
    }

    public ForgeDbCteInnerQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        _where.Add(ForgeDbFluentExpressionSql.TranslateWhere(predicate));
        return this;
    }

    internal string ToSql()
    {
        var sql = new StringBuilder($"SELECT * FROM {_table}");
        if (_where.Count > 0)
            sql.Append(" WHERE ").Append(string.Join(" AND ", _where));
        return sql.ToString();
    }
}
