namespace ForgeORM.Abstractions;

public sealed record ForgeModuleDescriptor(
    string Name,
    ForgeReleasePhase Phase,
    IReadOnlyList<ForgeFeatureDescriptor> Features);
