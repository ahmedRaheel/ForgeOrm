using System.Reflection;

namespace ForgeORM.Core;

public sealed record ForgeSchemaDiff(string Entity, IReadOnlyList<string> Statements);
