namespace ForgeORM.Core.Reliability;

public sealed class ForgeCircuitBreaker
{
    private int _failures;
    private DateTimeOffset? _openedUntil;

    public bool IsOpen => _openedUntil is not null && _openedUntil > DateTimeOffset.UtcNow;

    public void RecordSuccess()
    {
        _failures = 0;
        _openedUntil = null;
    }

    public void RecordFailure(int threshold = 5, TimeSpan? breakDuration = null)
    {
        _failures++;

        if (_failures >= threshold)
        {
            _openedUntil = DateTimeOffset.UtcNow.Add(breakDuration ?? TimeSpan.FromSeconds(30));
        }
    }
}
