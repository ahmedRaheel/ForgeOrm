using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ForgeORM.Core;
using ForgeORM.QueryAst;

namespace ForgeORM.Core.Search;

internal sealed record ForgeSearchExpressionResult(
    string Sql,
    IReadOnlyDictionary<string, object?> Parameters);
