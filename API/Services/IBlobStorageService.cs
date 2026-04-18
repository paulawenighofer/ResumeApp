namespace API.Services;

public interface IBlobStorageService
{
    Task<string> UploadProfileImageAsync(
        string userId,
        Stream imageStream,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<string> UploadResumePdfAsync(
        string userId,
        int resumeId,
        Stream pdfStream,
        CancellationToken cancellationToken = default);

    Task<ResumePdfDownloadResult?> DownloadResumePdfAsync(
        string blobPath,
        CancellationToken cancellationToken = default);

    Task<bool> ResumePdfExistsAsync(string blobPath, CancellationToken cancellationToken = default);

    Task<bool> DeleteResumePdfAsync(string blobPath, CancellationToken cancellationToken = default);

    Task<bool> TryDeleteAsync(string blobUrl, CancellationToken cancellationToken = default);
}
