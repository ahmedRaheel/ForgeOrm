namespace ForgeORM.Abstractions;

public sealed record ForgeFeatureDescriptor(
    string Code,
    string Name,
    ForgeReleasePhase Phase,
    ForgeFeatureStatus Status,
    string Description);
