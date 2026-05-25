using System.Collections;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

internal sealed record ForgeSplitIdFilter(string Predicate, object Parameters);
