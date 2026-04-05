using System.Collections.Concurrent;

namespace API.Services;

public sealed class UserActivityTracker
{
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSeen = new();

    public UserActivityTracker() : this(TimeSpan.FromMinutes(5))
    {
    }

    public UserActivityTracker(TimeSpan window)
    {
        _window = window;
    }

    public TimeSpan Window => _window;

    public void RecordActivity(string userId, DateTimeOffset? seenAt = null)
    {
        var timestamp = seenAt ?? DateTimeOffset.UtcNow;
        _lastSeen.AddOrUpdate(userId, timestamp, (_, current) => current > timestamp ? current : timestamp);
        Prune(timestamp);
    }

    public int GetActiveUserCount(DateTimeOffset? now = null)
    {
        var timestamp = now ?? DateTimeOffset.UtcNow;
        Prune(timestamp);
        var cutoff = timestamp - _window;
        return _lastSeen.Count(entry => entry.Value >= cutoff);
    }

    private void Prune(DateTimeOffset now)
    {
        var cutoff = now - _window;
        foreach (var entry in _lastSeen)
        {
            if (entry.Value < cutoff)
            {
                _lastSeen.TryRemove(entry.Key, out _);
            }
        }
    }
}
