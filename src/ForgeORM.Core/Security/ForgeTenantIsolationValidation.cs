using System.Text.RegularExpressions;

namespace ForgeORM.Core.Security;

public sealed record ForgeTenantIsolationValidation(
    bool IsIsolated,
    string Message);
