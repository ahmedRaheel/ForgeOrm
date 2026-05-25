using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ForgeORM.Core;

namespace ForgeORM.Querying.Search;

internal sealed record ForgeSearchExpressionResult(
    string Sql,
    IReadOnlyDictionary<string, object?> Parameters);
