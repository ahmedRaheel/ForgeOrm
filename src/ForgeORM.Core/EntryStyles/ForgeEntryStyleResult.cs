using System.Linq.Expressions;
using ForgeORM.Core.Materialization;

namespace ForgeORM.Core.EntryStyles;

/// <summary>
/// Standard query result wrapper for docs, samples and user-friendly APIs.
/// </summary>
public sealed record ForgeEntryStyleResult<T>(
    ForgeEntryStyle Style,
    string Description,
    T Result);
