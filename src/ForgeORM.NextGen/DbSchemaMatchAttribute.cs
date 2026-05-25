
using System.Runtime.CompilerServices;
using System.Text;
using ForgeORM.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ForgeORM.NextGen;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DbSchemaMatchAttribute : Attribute
{
    public string? TableName { get; init; }
    public bool FailOnDrift { get; init; } = true;
}
