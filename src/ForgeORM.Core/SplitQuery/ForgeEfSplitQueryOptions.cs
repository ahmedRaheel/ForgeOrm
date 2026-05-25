using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal sealed class ForgeEfSplitQueryOptions
{
    public static readonly ForgeEfSplitQueryOptions Default = new();
    public ForgeEfSplitQueryMode Mode { get; set; } = ForgeEfSplitQueryMode.SplitQuery;
    public bool UseIdentityResolution { get; set; }
    public List<LambdaExpression> ThenIncludes { get; } = [];
}
