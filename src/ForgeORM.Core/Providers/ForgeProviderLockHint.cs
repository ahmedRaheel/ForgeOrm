using System.Data;
using System.Text;

namespace ForgeORM.Core.Providers;

public enum ForgeProviderLockHint
{
    None,
    NoLock,
    ReadPast,
    UpdateLock,
    RowLock,
    SkipLocked
}
