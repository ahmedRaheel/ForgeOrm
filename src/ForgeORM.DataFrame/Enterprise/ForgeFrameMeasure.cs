namespace ForgeORM.DataFrame.Enterprise;

/// <summary>
/// Defines a DataFrame measure.
/// </summary>
public sealed record ForgeFrameMeasure(string Name, string Column, ForgeFrameAggregateKind Kind);
