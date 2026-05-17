# Program.cs registration

Add this line after the other `app.Map...Endpoints()` calls:

```csharp
app.MapEnterpriseAdvancedFeaturePackEndpoints();
```

If your sample project uses global usings or a shared endpoint registration file, add the same line there instead.
