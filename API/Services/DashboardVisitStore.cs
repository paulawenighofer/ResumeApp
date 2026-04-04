using System.Text.Json;

namespace API.Services;

public sealed class DashboardVisitStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;

    public DashboardVisitStore(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "dashboard-visits.json");
    }

    public async Task<DashboardVisitSnapshot> RecordVisitAsync(DateOnly date)
    {
        await _gate.WaitAsync();
        try
        {
            var snapshot = await LoadAsync();
            var key = date.ToString("yyyy-MM-dd");

            snapshot.TotalLoads++;
            snapshot.DailyLoads[key] = snapshot.DailyLoads.TryGetValue(key, out var current)
                ? current + 1
                : 1;

            await SaveAsync(snapshot);
            return snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DashboardVisitSnapshot> GetSnapshotAsync()
    {
        await _gate.WaitAsync();
        try
        {
            return await LoadAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<DashboardVisitSnapshot> LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            return new DashboardVisitSnapshot();
        }

        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<DashboardVisitSnapshot>(stream, JsonOptions)
            ?? new DashboardVisitSnapshot();
    }

    private async Task SaveAsync(DashboardVisitSnapshot snapshot)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions);
    }
}

public sealed class DashboardVisitSnapshot
{
    public int TotalLoads { get; set; }
    public Dictionary<string, int> DailyLoads { get; set; } = new(StringComparer.Ordinal);
}
