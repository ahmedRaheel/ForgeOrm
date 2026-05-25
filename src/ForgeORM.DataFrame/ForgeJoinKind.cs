using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ForgeORM.Core;
using Microsoft.Data.Analysis;


namespace ForgeORM.DataFrame;

public enum ForgeJoinKind { Inner, Left, Right, Full }
