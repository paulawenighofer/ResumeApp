using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace API.Services;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _profileImagesContainerClient;
    private readonly string _profileImagesBasePath;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IOptions<AzureBlobOptions> options, ILogger<AzureBlobStorageService> logger)
    {
        var settings = options.Value;
        _profileImagesContainerClient = new BlobContainerClient(settings.ConnectionString, settings.ProfileImagesContainer);
        _profileImagesBasePath = NormalizePathPrefix(settings.ProfileImagesBasePath);
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

    private static string NormalizePathPrefix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().Trim('/');
    }
}
