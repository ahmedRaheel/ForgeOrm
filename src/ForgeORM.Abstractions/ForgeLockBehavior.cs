namespace ForgeORM.Abstractions;

public enum ForgeLockBehavior
{
    None,
    NoLock,
    ReadPast,
    UpdateLock,
    RowLock,
    HoldLock
}
