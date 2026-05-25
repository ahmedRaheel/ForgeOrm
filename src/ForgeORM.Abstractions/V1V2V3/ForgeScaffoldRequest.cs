namespace ForgeORM.Abstractions;

public sealed record ForgeScaffoldRequest(string ConnectionString, string Provider, string Namespace, string OutputPath);
