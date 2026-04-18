using API.Services;

namespace Test.Integration.Fixtures;

public sealed class FakeBlobStorageService : IBlobStorageService
{
    private readonly Dictionary<string, byte[]> _resumePdfs = new(StringComparer.OrdinalIgnoreCase);

    public Task<string> UploadProfileImageAsync(string userId, Stream imageStream, string extension, string contentType, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"https://fake.local/profile-images/{userId}/{Guid.NewGuid():N}{extension}");
    }

    public async Task<string> UploadResumePdfAsync(string userId, int resumeId, Stream pdfStream, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms, cancellationToken);

        var path = $"users/{userId}/resumes/{resumeId}/{Guid.NewGuid():N}.pdf";
        _resumePdfs[path] = ms.ToArray();
        return path;
    }

    public Task<Stream?> DownloadResumePdfAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        if (!_resumePdfs.TryGetValue(blobPath, out var bytes))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new MemoryStream(bytes, writable: false);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> TryDeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
