using System.Linq.Expressions;
using ForgeORM.Core.Materialization;

namespace ForgeORM.Core.EntryStyles;

/// <summary>
/// ForgeORM standard user experience:
/// every major feature should be available through three styles:
/// 1. Fluent/query builder
/// 2. Raw SQL
/// 3. Expression-based
/// </summary>
public enum ForgeEntryStyle
{
    FluentBuilder,
    RawSql,
    Expression
}
