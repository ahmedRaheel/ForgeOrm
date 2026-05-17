# Enum Materializer Search Fix Applied

Fixed runtime error:

```text
System.ArgumentException: Requested value 'Processing' was not found.
```

Cause:
- `ForgeValueConverter.FromDatabase` used `Enum.Parse(...)`.
- If the database contained an enum string that was not currently defined in the C# enum, materialization crashed.

Fix:
- Replaced `Enum.Parse` with safe enum conversion:
  - case-insensitive `Enum.TryParse`
  - numeric text support
  - numeric database value support
  - fallback to `Unknown`, `None`, or `Default` enum member if present
  - final fallback to enum default value

This keeps search endpoints working even when database enum values drift from the current code enum.
