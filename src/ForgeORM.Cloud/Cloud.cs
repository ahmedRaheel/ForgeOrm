using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Cloud;

public sealed record CloudDeploymentRequest(string AppName, string Provider, string ContainerImage, int Replicas = 2, string? Region = null);
public sealed record CloudDeploymentArtifacts(string Dockerfile, string KubernetesYaml, string Terraform, string HelmValues);

public interface IForgeDeploymentGenerator
{
    CloudDeploymentArtifacts Generate(CloudDeploymentRequest request);
}

public sealed class ForgeDeploymentGenerator : IForgeDeploymentGenerator
{
    /// <summary>
    /// Initializes or executes the Generate operation.
    /// </summary>
    /// <param name="r">The r value.</param>
    /// <returns>The operation result.</returns>
    public CloudDeploymentArtifacts Generate(CloudDeploymentRequest r)
    {
        var docker = $"FROM mcr.microsoft.com/dotnet/aspnet:10.0\nWORKDIR /app\nCOPY . .\nENTRYPOINT [\"dotnet\", \"{r.AppName}.dll\"]";
        var k8s = $"apiVersion: apps/v1\nkind: Deployment\nmetadata:\n  name: {r.AppName.ToLowerInvariant()}\nspec:\n  replicas: {r.Replicas}\n  selector:\n    matchLabels:\n      app: {r.AppName.ToLowerInvariant()}\n  template:\n    metadata:\n      labels:\n        app: {r.AppName.ToLowerInvariant()}\n    spec:\n      containers:\n      - name: api\n        image: {r.ContainerImage}\n        ports:\n        - containerPort: 8080";
        var tf = $"# Terraform placeholder for {r.Provider}\nvariable \"region\" {{ default = \"{r.Region ?? "eastus"}\" }}";
        var helm = $"image:\n  repository: {r.ContainerImage}\nreplicaCount: {r.Replicas}";
        return new CloudDeploymentArtifacts(docker, k8s, tf, helm);
    }
}

public static class ForgeCloudServiceCollectionExtensions
{
    /// <summary>
    /// Initializes or executes the AddForgeCloudDeployment operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The operation result.</returns>
    public static IServiceCollection AddForgeCloudDeployment(this IServiceCollection services) => services.AddSingleton<IForgeDeploymentGenerator, ForgeDeploymentGenerator>();
}
