using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace API.Services;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private const int MaxRetryAttempts = 3;

    private readonly BlobContainerClient _profileImagesContainerClient;
    private readonly BlobContainerClient _resumesContainerClient;
    private readonly string _profileImagesBasePath;
    private readonly string _resumesBasePath;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly ApiMetrics _metrics;

    public AzureBlobStorageService(IOptions<AzureBlobOptions> options, ILogger<AzureBlobStorageService> logger, ApiMetrics metrics)
    {
        var settings = options.Value;
        var clientOptions = new BlobClientOptions
        {
            Retry =
            {
                Delay = TimeSpan.FromMilliseconds(200),
                MaxDelay = TimeSpan.FromSeconds(2),
                MaxRetries = MaxRetryAttempts - 1,
                Mode = RetryMode.Exponential
            }
        };

        _profileImagesContainerClient = new BlobContainerClient(settings.ConnectionString, settings.ProfileImagesContainer, clientOptions);
        _resumesContainerClient = new BlobContainerClient(settings.ConnectionString, settings.ResumesContainer, clientOptions);
        _profileImagesBasePath = NormalizePathPrefix(settings.ProfileImagesBasePath);
        _resumesBasePath = NormalizePathPrefix(settings.ResumesBasePath);
        _logger = logger;
        _metrics = metrics;
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
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            };

            await ExecuteWithRetryAsync(
                "profile_image",
                "upload",
                userId,
                () => blobClient.UploadAsync(imageStream, uploadOptions, cancellationToken),
                cancellationToken);

            stopwatch.Stop();
            _metrics.RecordBlobOperation("profile_image", "upload", "success", stopwatch.Elapsed.TotalMilliseconds, userId);
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordBlobOperation("profile_image", "upload", "failure", stopwatch.Elapsed.TotalMilliseconds, userId);
            _logger.LogWarning(ex, "Profile image upload failed for user {UserId} and blob {BlobName}", userId, blobName);
            throw;
        }
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
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "application/pdf"
                }
            };

            if (pdfStream.CanSeek)
            {
                pdfStream.Position = 0;
            }

            await ExecuteWithRetryAsync(
                "resume_pdf",
                "upload",
                userId,
                () => blobClient.UploadAsync(pdfStream, uploadOptions, cancellationToken),
                cancellationToken);

            stopwatch.Stop();
            _metrics.RecordBlobOperation("resume_pdf", "upload", "success", stopwatch.Elapsed.TotalMilliseconds, userId);
            return blobName;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordBlobOperation("resume_pdf", "upload", "failure", stopwatch.Elapsed.TotalMilliseconds, userId);
            _logger.LogWarning(ex, "Resume PDF upload failed for user {UserId} and resume {ResumeId}", userId, resumeId);
            throw;
        }
    }

    public async Task<ResumePdfDownloadResult?> DownloadResumePdfAsync(
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return null;
        }

        var blobClient = _resumesContainerClient.GetBlobClient(blobPath);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var properties = await ExecuteWithRetryAsync(
                "resume_pdf",
                "metadata",
                null,
                () => blobClient.GetPropertiesAsync(cancellationToken: cancellationToken),
                cancellationToken);

            var contentType = string.IsNullOrWhiteSpace(properties.Value.ContentType)
                ? "application/pdf"
                : properties.Value.ContentType;

            if (!string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Resume PDF blob {BlobPath} had unexpected content type {ContentType}", blobPath, contentType);
                return null;
            }

            var response = await ExecuteWithRetryAsync(
                "resume_pdf",
                "download",
                null,
                () => blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken),
                cancellationToken);

            var memory = new MemoryStream();
            await response.Value.Content.CopyToAsync(memory, cancellationToken);
            memory.Position = 0;

            stopwatch.Stop();
            _metrics.RecordBlobOperation("resume_pdf", "download", "success", stopwatch.Elapsed.TotalMilliseconds);

            var fileName = Path.GetFileName(blobPath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "resume.pdf";
            }

            return new ResumePdfDownloadResult(memory, fileName, contentType);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            stopwatch.Stop();
            _metrics.RecordBlobOperation("resume_pdf", "download", "failure", stopwatch.Elapsed.TotalMilliseconds);
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordBlobOperation("resume_pdf", "download", "failure", stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogWarning(ex, "Resume PDF download failed for blob {BlobPath}", blobPath);
            throw;
        }
    }

    public async Task<bool> ResumePdfExistsAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return false;
        }

        var blobClient = _resumesContainerClient.GetBlobClient(blobPath);
        var response = await ExecuteWithRetryAsync(
            "resume_pdf",
            "exists",
            null,
            () => blobClient.ExistsAsync(cancellationToken),
            cancellationToken);
        return response.Value;
    }

    public async Task<bool> DeleteResumePdfAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return false;
        }

        var blobClient = _resumesContainerClient.GetBlobClient(blobPath);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await ExecuteWithRetryAsync(
                "resume_pdf",
                "delete",
                null,
                () => blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken),
                cancellationToken);

            stopwatch.Stop();
            _metrics.RecordBlobOperation("resume_pdf", "delete", response.Value ? "success" : "not_found", stopwatch.Elapsed.TotalMilliseconds);
            return response.Value;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordBlobOperation("resume_pdf", "delete", "failure", stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogWarning(ex, "Failed to delete resume PDF blob {BlobPath}", blobPath);
            return false;
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

            var stopwatch = Stopwatch.StartNew();
            var response = await ExecuteWithRetryAsync(
                "profile_image",
                "delete",
                null,
                () => _profileImagesContainerClient.DeleteBlobIfExistsAsync(
                    blobClient.Name,
                    DeleteSnapshotsOption.IncludeSnapshots,
                    cancellationToken: cancellationToken),
                cancellationToken);

            stopwatch.Stop();
            _metrics.RecordBlobOperation("profile_image", "delete", response.Value ? "success" : "not_found", stopwatch.Elapsed.TotalMilliseconds);
            return response.Value;
        }
        catch (Exception ex)
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

    private static async Task<T> ExecuteWithRetryAsync<T>(
        string category,
        string operation,
        string? userId,
        Func<Task<T>> operationFactory,
        CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMilliseconds(200);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operationFactory();
            }
            catch (Exception ex) when (attempt < MaxRetryAttempts && IsTransient(ex))
            {
                lastException = ex;
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }

        if (lastException is not null)
        {
            throw lastException;
        }

        throw new InvalidOperationException($"{category}:{operation} failed without an exception.");
    }

    private static bool IsTransient(Exception ex)
    {
        return ex is HttpRequestException
            or TaskCanceledException
            or TimeoutException
            or RequestFailedException { Status: 408 }
            or RequestFailedException { Status: 429 }
            or RequestFailedException { Status: >= 500 and <= 599 };
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
