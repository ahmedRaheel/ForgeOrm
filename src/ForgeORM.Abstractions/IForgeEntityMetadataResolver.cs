using System;
using System.Data.Common;
using System.Linq.Expressions;

namespace ForgeORM.Abstractions;

public interface IForgeEntityMetadataResolver
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    ForgeEntityMetadata Resolve<T>();
    /// <summary>
    /// Defines the Resolve operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the Resolve operation.</returns>
    ForgeEntityMetadata Resolve(Type type);
}
