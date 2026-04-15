using ResumeApp.Models;

namespace ResumeApp.Services;

public interface ILocalStorageService
{
    Task InitializeAsync();
    Task<List<T>> LoadItemsAsync<T>(bool includeDeleted = false) where T : class, ILocalSyncEntity, new();
    Task SaveItemAsync<T>(T item) where T : class, ILocalSyncEntity, new();
    Task SaveItemsAsync<T>(IEnumerable<T> items) where T : class, ILocalSyncEntity, new();
    Task DeleteItemAsync<T>(T item) where T : class, ILocalSyncEntity, new();
    Task SaveProfileImagePathAsync(string? imagePath);
    Task<string?> LoadProfileImagePathAsync();
}
