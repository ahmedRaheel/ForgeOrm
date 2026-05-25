using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public static class ForgeJobFacadeExtensions
{
    private static readonly ConditionalWeakTable<ForgeDb, ForgeJobFacade> JobsCache = new();
    public static ForgeJobFacade Jobs(this ForgeDb db) => JobsCache.GetValue(db, static x => new ForgeJobFacade(x));
}
