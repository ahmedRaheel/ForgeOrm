using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Cloud;

public sealed record CloudDeploymentArtifacts(string Dockerfile, string KubernetesYaml, string Terraform, string HelmValues);
