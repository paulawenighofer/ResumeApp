using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResumeApp.Data;
using ResumeApp.Models;

namespace ResumeApp.Services;

public class LocalStorageService : ILocalStorageService
{
    private const string ProfileImageKey = "profile_image_path";
    private readonly IDbContextFactory<OfflineAppDbContext> _contextFactory;

    public LocalStorageService(IDbContextFactory<OfflineAppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task InitializeAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task<List<T>> LoadItemsAsync<T>(bool includeDeleted = false) where T : class, ILocalSyncEntity, new()
    {
        await InitializeAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var items = await context.Set<T>().AsNoTracking().ToListAsync();
        return items
            .Where(x => includeDeleted || !x.Deleted)
            .OrderByDescending(x => x.UpdatedAt ?? DateTimeOffset.MinValue)
            .ToList();
    }

    public async Task SaveItemAsync<T>(T item) where T : class, ILocalSyncEntity, new()
    {
        await InitializeAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Set<T>().FindAsync(item.Id);
        if (existing is null)
        {
            await context.Set<T>().AddAsync(item);
        }
        else
        {
            context.Entry(existing).CurrentValues.SetValues(item);
        }

        await context.SaveChangesAsync();
    }

    public async Task SaveItemsAsync<T>(IEnumerable<T> items) where T : class, ILocalSyncEntity, new()
    {
        foreach (var item in items)
        {
            await SaveItemAsync(item);
        }
    }

    public async Task DeleteItemAsync<T>(T item) where T : class, ILocalSyncEntity, new()
    {
        await InitializeAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Set<T>().FindAsync(item.Id);
        if (existing is null)
        {
            return;
        }

        context.Remove(existing);
        await context.SaveChangesAsync();
    }

    public Task SaveProfileImagePathAsync(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            Preferences.Default.Remove(ProfileImageKey);
        }
        else
        {
            Preferences.Default.Set(ProfileImageKey, imagePath);
        }

        return Task.CompletedTask;
    }

    public Task<string?> LoadProfileImagePathAsync()
        => Task.FromResult<string?>(Preferences.Default.ContainsKey(ProfileImageKey)
            ? Preferences.Default.Get(ProfileImageKey, string.Empty)
            : null);
}
