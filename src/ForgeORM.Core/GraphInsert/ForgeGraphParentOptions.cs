using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

public sealed class ForgeGraphParentOptions<TParent>
{
    internal PropertyInfo? KeyProperty { get; set; }

    /// <summary>
    /// Executes the TKey operation.
    /// </summary>
    /// <typeparam name="TKey">The type used by the operation.</typeparam>
    /// <param name="key">The key value.</param>
    /// <returns>The result of the TKey operation.</returns>
    public ForgeGraphParentOptions<TParent> Key<TKey>(Expression<Func<TParent, TKey>> key)
    {
        KeyProperty = ForgeExpression.Property(key.Body);
        return this;
    }
}
