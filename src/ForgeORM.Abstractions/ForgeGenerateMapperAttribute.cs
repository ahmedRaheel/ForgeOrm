namespace ForgeORM.Abstractions;

/// <summary>
/// Opts a DTO/entity into ForgeORM compile-time source generation even when it is not decorated with ForgeTable.
/// Useful for read models, immutable records and projection DTOs.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ForgeGenerateMapperAttribute : Attribute
{
}
