using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Cloud;

public sealed record CloudDeploymentRequest(string AppName, string Provider, string ContainerImage, int Replicas = 2, string? Region = null);
