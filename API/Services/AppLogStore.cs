using System.Collections.Concurrent;

namespace API.Services;

public sealed class AppLogStore
{
    private readonly ConcurrentQueue<DashboardLogEntry> _entries = new();
    private readonly SemaphoreSlim _signal = new(0);
    private long _nextId;
    private const int MaxEntries = 250;

    public void Add(string level, string category, string message)
    {
        var entry = new DashboardLogEntry
        {
            Id = Interlocked.Increment(ref _nextId),
            TimestampUtc = DateTime.UtcNow,
            Level = level,
            Category = category,
            Message = message
        };

        _entries.Enqueue(entry);

        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _))
        {
        }

        _signal.Release();
    }

    public IReadOnlyList<DashboardLogEntry> GetRecent(int count = 80)
    {
        return _entries
            .ToArray()
            .OrderByDescending(x => x.Id)
            .Take(count)
            .OrderBy(x => x.Id)
            .ToList();
    }

    public IReadOnlyList<DashboardLogEntry> GetEntriesAfter(long lastSeenId)
    {
        return _entries
            .ToArray()
            .Where(x => x.Id > lastSeenId)
            .OrderBy(x => x.Id)
            .ToList();
    }

    public async Task WaitForNewEntriesAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
    }
}

public sealed class DashboardLogEntry
{
    public long Id { get; init; }
    public DateTime TimestampUtc { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
