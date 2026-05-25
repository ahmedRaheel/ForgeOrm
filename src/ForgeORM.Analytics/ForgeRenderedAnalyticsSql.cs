using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ForgeORM.Abstractions;
using ForgeORM.Core;

namespace ForgeORM.Analytics;

public sealed record ForgeRenderedAnalyticsSql(string Sql);
