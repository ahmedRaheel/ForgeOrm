using System.Linq.Expressions;
using System.Threading.Tasks;
using ForgeORM.Core;

namespace ForgeORM.DataFrame;

public sealed class ForgeVectorizedFrame<T>
{
    private readonly ForgeDataFrame _frame;

    internal ForgeVectorizedFrame(ForgeDataFrame frame) => _frame = frame;

    public ForgeVectorizedFrame<T> Where(Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        var rows = _frame.Rows.Where(row => compiled(ToObject(row.ToDictionary()))).Select(row => new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase));
        return new ForgeVectorizedFrame<T>(new ForgeDataFrame(rows));
    }

    public decimal Sum(Expression<Func<T, decimal>> selector)
    {
        var column = ForgeFrameExpressionSql.GetMemberName(selector.Body);
        return _frame.Rows
            .Select(r => ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(r, column)))
            .Where(x => x.HasValue)
            .Sum(x => x!.Value);
    }

    public decimal Sum(Expression<Func<T, decimal?>> selector)
    {
        var column = ForgeFrameExpressionSql.GetMemberName(selector.Body);
        return _frame.Rows
            .Select(r => ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(r, column)))
            .Where(x => x.HasValue)
            .Sum(x => x!.Value);
    }

    public decimal? Average(Expression<Func<T, decimal>> selector)
    {
        var column = ForgeFrameExpressionSql.GetMemberName(selector.Body);
        var values = _frame.Rows
            .Select(r => ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(r, column)))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToArray();
        return values.Length == 0 ? null : values.Average();
    }

    public int Count(Expression<Func<T, object?>>? selector = null)
    {
        if (selector is null) return _frame.Rows.Count;
        var column = ForgeFrameExpressionSql.GetMemberName(selector.Body);
        return _frame.Rows.Count(r => ForgeDataFrame.Get(r, column) is not null and not DBNull);
    }

    private static T ToObject(IDictionary<string, object?> row)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(row);
        return System.Text.Json.JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidOperationException($"Unable to create {typeof(T).Name} from ForgeDataFrame row.");
    }
}
