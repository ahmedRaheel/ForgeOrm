namespace ForgeORM.Core.Materialization;

/// <summary>
/// Compatibility marker. Dynamic materialization is now implemented directly on ForgeDb
/// so extension methods do not need to access private CreateConnection().
/// </summary>
internal static class ForgeDbDynamicMaterializationExtensionsCompatibility
{
}
