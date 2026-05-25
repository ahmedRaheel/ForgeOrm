namespace ForgeORM.Abstractions;

public enum ForgeReadConsistency
{
    Default,
    ReadCommitted,
    ReadUncommitted,
    Snapshot,
    Serializable
}
