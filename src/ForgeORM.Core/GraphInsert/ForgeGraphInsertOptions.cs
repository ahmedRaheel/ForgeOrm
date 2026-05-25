using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

public sealed class ForgeGraphInsertOptions<TParent, TDto>
{
    internal ForgeGraphParentOptions<TParent> ParentOptions { get; } = new();
    internal List<IForgeGraphChildInsert<TDto>> ChildMappings { get; } = [];

    /// <summary>Gets or sets whether child mappings should be inserted.</summary>
    public bool IncludeChildren { get; set; } = true;

    /// <summary>Gets or sets whether bulk strategies may be used for children.</summary>
    public bool UseBulkWhenPossible { get; set; } = true;

    /// <summary>Gets or sets the preferred batch size for child inserts.</summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>Gets or sets the preferred child insert strategy.</summary>
    public ForgeBulkStrategy Strategy { get; set; } = ForgeBulkStrategy.Auto;

    /// <summary>
    /// Executes the Parent operation.
    /// </summary>
    /// <returns>The result of the Parent operation.</returns>
    public ForgeGraphParentOptions<TParent> Parent() => ParentOptions;

    /// <summary>
    /// Executes the TChildDto operation.
    /// </summary>
    /// <typeparam name="TChildEntity">The type used by the operation.</typeparam>
    /// <typeparam name="TChildDto">The type used by the operation.</typeparam>
    /// <param name="selector">The selector value.</param>
    /// <returns>The result of the TChildDto operation.</returns>
    public ForgeGraphChildOptions<TDto, TChildEntity, TChildDto> ChildrenOf<TChildEntity, TChildDto>(
        Expression<Func<TDto, IEnumerable<TChildDto>>> selector)
        where TChildEntity : new()
    {
        var options = new ForgeGraphChildOptions<TDto, TChildEntity, TChildDto>(ForgeExpressionDelegateCache.Get(selector));
        ChildMappings.Add(options);
        return options;
    }

    /// <summary>
    /// Executes the TChildDto operation.
    /// </summary>
    /// <typeparam name="TChildEntity">The type used by the operation.</typeparam>
    /// <typeparam name="TChildDto">The type used by the operation.</typeparam>
    /// <param name="selector">The selector value.</param>
    /// <returns>The result of the TChildDto operation.</returns>
    public ForgeGraphChildOptions<TDto, TChildEntity, TChildDto> Children<TChildEntity, TChildDto>(
        Expression<Func<TDto, IEnumerable<TChildDto>>> selector)
        where TChildEntity : new()
        => ChildrenOf<TChildEntity, TChildDto>(selector);
}
