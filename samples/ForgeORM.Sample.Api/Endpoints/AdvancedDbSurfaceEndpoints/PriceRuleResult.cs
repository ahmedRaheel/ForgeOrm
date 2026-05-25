using System.Buffers;
using ForgeORM.Core;
using ForgeORM.Core.Search;
using ForgeORM.DataFrame;

public sealed record PriceRuleResult(decimal Price = 0m, string Currency = "USD", string Rule = "Default");
