using System;
using System.Data.Common;
using System.Linq.Expressions;

namespace ForgeORM.Abstractions;

/// <summary>
/// Query returned after Include so callers can continue with ThenInclude while retaining the normal Forge query API.
/// </summary>
public interface IForgeIncludableQuery<T, TProperty> : IForgeQuery<T>
{
    IForgeQuery<T> Query { get; }
}
