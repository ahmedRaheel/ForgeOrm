using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    private ForgeJobFacade? _jobs;

    /// <summary>Background job facade exposed directly as db.Jobs.</summary>
    public ForgeJobFacade Jobs => _jobs ??= new ForgeJobFacade(this);
}
