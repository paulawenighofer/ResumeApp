using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace API.Services;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _profileImagesContainerClient;
    private readonly BlobContainerClient _resumesContainerClient;
    private readonly string _profileImagesBasePath;
    private readonly string _resumesBasePath;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IOptions<AzureBlobOptions> options, ILogger<AzureBlobStorageService> logger)
    {
        var settings = options.Value;
        _profileImagesContainerClient = new BlobContainerClient(settings.ConnectionString, settings.ProfileImagesContainer);
        _resumesContainerClient = new BlobContainerClient(settings.ConnectionString, settings.ResumesContainer);
        _profileImagesBasePath = NormalizePathPrefix(settings.ProfileImagesBasePath);
        _resumesBasePath = NormalizePathPrefix(settings.ResumesBasePath);
        _logger = logger;
    }

    public async Task<string> UploadProfileImageAsync(
        string userId,
        Stream imageStream,
        string extension,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await _profileImagesContainerClient.CreateIfNotExistsAsync(
            PublicAccessType.Blob,
            cancellationToken: cancellationToken);

        var blobName = BuildProfileImageBlobName(userId, extension);
        var blobClient = _profileImagesContainerClient.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            }
        };

        await blobClient.UploadAsync(imageStream, uploadOptions, cancellationToken);
        return blobClient.Uri.ToString();
    }

    public async Task<string> UploadResumePdfAsync(
        string userId,
        int resumeId,
        Stream pdfStream,
        CancellationToken cancellationToken = default)
    {
        await _resumesContainerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        var blobName = BuildResumePdfBlobName(userId, resumeId);
        var blobClient = _resumesContainerClient.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = "application/pdf"
            }
        };

        await blobClient.UploadAsync(pdfStream, uploadOptions, cancellationToken);
        return blobName;
    }

    public async Task<Stream?> DownloadResumePdfAsync(
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return null;
        }

        var blobClient = _resumesContainerClient.GetBlobClient(blobPath);

        try
        {
            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            var memory = new MemoryStream();
            await response.Value.Content.CopyToAsync(memory, cancellationToken);
            memory.Position = 0;
            return memory;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> TryDeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var blobUri))
        {
            return false;
        }

        try
        {
            var blobClient = new BlobClient(blobUri);
            if (!string.Equals(blobClient.BlobContainerName, _profileImagesContainerClient.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var response = await _profileImagesContainerClient.DeleteBlobIfExistsAsync(
                blobClient.Name,
                DeleteSnapshotsOption.IncludeSnapshots,
                cancellationToken: cancellationToken);

            return response.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(ex, "Failed to delete existing profile image blob {BlobUrl}", blobUrl);
            return false;
        }
    }

    private string BuildProfileImageBlobName(string userId, string extension)
    {
        var cleanExtension = extension.StartsWith('.') ? extension : $".{extension}";
        var relativePath = $"users/{userId}/profile/{Guid.NewGuid():N}{cleanExtension}";

        return string.IsNullOrWhiteSpace(_profileImagesBasePath)
            ? relativePath
            : $"{_profileImagesBasePath}/{relativePath}";
    }

    private string BuildResumePdfBlobName(string userId, int resumeId)
    {
        var relativePath = $"users/{userId}/resumes/{resumeId}/final.pdf";

        return string.IsNullOrWhiteSpace(_resumesBasePath)
            ? relativePath
            : $"{_resumesBasePath}/{relativePath}";
    }

    private static string NormalizePathPrefix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().Trim('/');
    }
}
