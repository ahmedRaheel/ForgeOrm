using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ForgeORM.Core;
using ForgeORM.QueryAst;

namespace ForgeORM.Core.Search;

public sealed class ForgePagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalRecords { get; init; }

    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalRecords / (double)PageSize);
}
