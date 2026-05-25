using System.Linq.Expressions;
using System.Text;

namespace ForgeORM.Core;

public sealed class ForgeDbTempTableQuery<T>
{
    private readonly ForgeDb _db;
    private readonly string _name;
    private string? _sourceSql;

    internal ForgeDbTempTableQuery(ForgeDb db, string name)
    {
        _db = db;
        _name = name.StartsWith("#", StringComparison.Ordinal) ? name : "#" + name;
    }

    public ForgeDbTempTableQuery<T> FromQuery(Func<ForgeDbTempSourceQuery<T>, ForgeDbTempSourceQuery<T>> build)
    {
        if (build is null) throw new ArgumentNullException(nameof(build));
        _sourceSql = build(new ForgeDbTempSourceQuery<T>()).ToSql();
        return this;
    }

    public string ToSql()
    {
        var source = string.IsNullOrWhiteSpace(_sourceSql) ? $"SELECT * FROM {typeof(T).Name}" : _sourceSql;
        return $"SELECT * INTO {_name} FROM ({source}) AS ForgeTempSource";
    }

    public ValueTask<int> CreateAsync(CancellationToken cancellationToken = default)
        => _db.ExecuteAsync(ToSql(), cancellationToken: cancellationToken);
}
