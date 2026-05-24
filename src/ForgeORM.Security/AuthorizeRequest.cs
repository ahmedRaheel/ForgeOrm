using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Security;
public sealed record AuthorizeRequest
{
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public required string UserId { get; init; }
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public required string Resource { get; init; }
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public required string Action { get; init; }
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public string? TenantId { get; init; }
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public IReadOnlyList<string> Roles { get; init; }        = [];
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public IReadOnlyDictionary<string, string> Claims { get; init; }   = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTimeOffset RequestedAt { get; init; }  = DateTimeOffset.UtcNow;
    public CancellationToken CancellationToken { get; init; }
}
