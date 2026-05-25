using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public sealed record ForgeMigrationPlan(string Name, IReadOnlyList<string> UpSql, IReadOnlyList<string> DownSql, IReadOnlyList<string> Warnings);
