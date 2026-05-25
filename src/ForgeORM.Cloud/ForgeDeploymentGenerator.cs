using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Cloud;

public sealed class ForgeDeploymentGenerator : IForgeDeploymentGenerator
{
    /// <summary>
    /// Executes the Generate operation.
    /// </summary>
    /// <param name="r">The r value.</param>
    /// <returns>The result of the Generate operation.</returns>
    public CloudDeploymentArtifacts Generate(CloudDeploymentRequest r)
    {
        var docker = $"FROM mcr.microsoft.com/dotnet/aspnet:10.0\nWORKDIR /app\nCOPY . .\nENTRYPOINT [\"dotnet\", \"{r.AppName}.dll\"]";
        var k8s = $"apiVersion: apps/v1\nkind: Deployment\nmetadata:\n  name: {r.AppName.ToLowerInvariant()}\nspec:\n  replicas: {r.Replicas}\n  selector:\n    matchLabels:\n      app: {r.AppName.ToLowerInvariant()}\n  template:\n    metadata:\n      labels:\n        app: {r.AppName.ToLowerInvariant()}\n    spec:\n      containers:\n      - name: api\n        image: {r.ContainerImage}\n        ports:\n        - containerPort: 8080";
        var tf = $"# Terraform placeholder for {r.Provider}\nvariable \"region\" {{ default = \"{r.Region ?? "eastus"}\" }}";
        var helm = $"image:\n  repository: {r.ContainerImage}\nreplicaCount: {r.Replicas}";
        return new CloudDeploymentArtifacts(docker, k8s, tf, helm);
    }
}
