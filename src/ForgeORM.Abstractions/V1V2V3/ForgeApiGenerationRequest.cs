namespace ForgeORM.Abstractions;

public sealed record ForgeApiGenerationRequest(string EntityName, string RoutePrefix, string Namespace);
