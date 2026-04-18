namespace API.Services;

public interface IBlobStorageService
{
    Task<string> UploadProfileImageAsync(
        string userId,
        Stream imageStream,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<bool> TryDeleteAsync(string blobUrl, CancellationToken cancellationToken = default);
}
