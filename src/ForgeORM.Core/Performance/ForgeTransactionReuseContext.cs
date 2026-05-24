using System.Data.Common;

namespace ForgeORM.Core;

/// <summary>
/// Async-local transaction reuse context used by graph, bulk, workflow and job pipelines.
/// </summary>
public sealed class ForgeTransactionReuseContext : IAsyncDisposable
{
    private static readonly AsyncLocal<ForgeTransactionReuseContext?> CurrentSlot = new();

    private readonly ForgeTransactionReuseContext? _previous;

    public DbConnection Connection { get; }
    public DbTransaction Transaction { get; }

    private ForgeTransactionReuseContext(DbConnection connection, DbTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
        _previous = CurrentSlot.Value;
        CurrentSlot.Value = this;
    }

    public static ForgeTransactionReuseContext? Current => CurrentSlot.Value;

    public static async ValueTask<ForgeTransactionReuseContext> BeginAsync(DbConnection connection, CancellationToken cancellationToken = default)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var tx = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        return new ForgeTransactionReuseContext(connection, tx);
    }

    public ValueTask CommitAsync(CancellationToken cancellationToken = default)
        => new( Transaction.CommitAsync(cancellationToken));

    public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
        => new(Transaction.RollbackAsync(cancellationToken));

    public async ValueTask DisposeAsync()
    {
        CurrentSlot.Value = _previous;
        await Transaction.DisposeAsync().ConfigureAwait(false);
    }
}
