using API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.DTO;
using Shared.Models;
using System.Diagnostics;
using System.Linq.Expressions;

namespace API.Services;

public class ResumeDraftService : IResumeDraftService
{
    private static readonly TimeSpan StaleDraftAge = TimeSpan.FromHours(24);
    private const int MaxTransientAttempts = 3;

    private readonly AppDbContext _db;
    private readonly IResumeProfileAssembler _profileAssembler;
    private readonly IAiResumeGenerationClient _aiClient;
    private readonly IResumeJsonValidator _jsonValidator;
    private readonly IPdfRenderer _pdfRenderer;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IResumeDraftGenerationQueue _generationQueue;
    private readonly ResumeDraftProcessingOptions _processingOptions;
    private readonly ApiMetrics _metrics;
    private readonly ILogger<ResumeDraftService> _logger;

    public ResumeDraftService(
        AppDbContext db,
        IResumeProfileAssembler profileAssembler,
        IAiResumeGenerationClient aiClient,
        IResumeJsonValidator jsonValidator,
        IPdfRenderer pdfRenderer,
        IBlobStorageService blobStorageService,
        IResumeDraftGenerationQueue generationQueue,
        IOptions<ResumeDraftProcessingOptions> processingOptions,
        ApiMetrics metrics,
        ILogger<ResumeDraftService> logger)
    {
        _db = db;
        _profileAssembler = profileAssembler;
        _aiClient = aiClient;
        _jsonValidator = jsonValidator;
        _pdfRenderer = pdfRenderer;
        _blobStorageService = blobStorageService;
        _generationQueue = generationQueue;
        _processingOptions = processingOptions.Value;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<ResumeDraftResponse> CreateDraftAsync(string userId, CreateResumeDraftRequest request, CancellationToken cancellationToken = default)
    {
        var assembled = await _profileAssembler.AssembleAsync(userId, request, cancellationToken);

        var draft = new Resume
        {
            UserId = userId,
            Status = ResumeDraftStatus.Pending,
            TargetCompany = request.TargetCompany,
            GenerationRequestJson = assembled.GenerationRequestJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Resumes.Add(draft);
        await _db.SaveChangesAsync(cancellationToken);

        if (_processingOptions.ProcessInBackground)
        {
            await _generationQueue.EnqueueAsync(new ResumeDraftGenerationWorkItem(userId, draft.Id, assembled.Prompt), cancellationToken);
            _logger.LogInformation("Resume draft queued for background generation for user {UserId} and resume {ResumeId}", userId, draft.Id);
        }
        else
        {
            await ProcessDraftGenerationAsync(userId, draft.Id, assembled.Prompt, cancellationToken);
            await _db.Entry(draft).ReloadAsync(cancellationToken);
        }

        return MapToDraftResponse(draft);
    }

    public async Task ProcessDraftGenerationAsync(string userId, int resumeId, string prompt, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var draft = await _db.Resumes.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == resumeId, cancellationToken);
        if (draft is null || draft.Status != ResumeDraftStatus.Pending)
        {
            return;
        }

        try
        {
            var generatedJson = await ExecuteWithRetryAsync(
                "draft_generation",
                userId,
                draft.Id,
                () => _aiClient.GenerateResumeJsonAsync(prompt, cancellationToken),
                cancellationToken);

            if (!_jsonValidator.TryValidate(generatedJson, out var failureReason))
            {
                throw new InvalidOperationException(failureReason ?? "AI output validation failed.");
            }

            draft.GeneratedResumeJson = generatedJson;
            draft.Status = ResumeDraftStatus.Generated;
            draft.FailedReason = null;
            _logger.LogInformation("Resume draft generated for user {UserId} and resume {ResumeId}", userId, draft.Id);
        }
        catch (Exception ex)
        {
            draft.Status = ResumeDraftStatus.DraftFailed;
            draft.FailedReason = Truncate(ex.Message);
            _logger.LogWarning(ex, "Resume draft generation failed for user {UserId} and resume {ResumeId}", userId, draft.Id);
        }

        draft.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();
        _metrics.RecordResumeDraftGeneration(
            draft.Status is ResumeDraftStatus.Generated ? "success" : "failure",
            stopwatch.Elapsed.TotalMilliseconds,
            userId);
    }

    public async Task<IReadOnlyList<ResumeListItemDto>> ListDraftsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _db.Resumes
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ResumeListItemDto
            {
                Id = x.Id,
                Status = x.Status,
                TargetCompany = x.TargetCompany,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ResumeDetailDto?> GetDraftAsync(string userId, int id, CancellationToken cancellationToken = default)
    {
        var resume = await _db.Resumes.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);
        if (resume is null)
        {
            return null;
        }

        var changed = await NormalizeResumeStateAsync(resume, cancellationToken);
        if (changed)
        {
            resume.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return MapToDetailDto(resume);
    }

    public async Task<ResumeDetailDto?> SaveDraftEditAsync(string userId, int id, SaveDraftEditRequest request, CancellationToken cancellationToken = default)
    {
        var resume = await _db.Resumes.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);

        if (resume is null)
        {
            return null;
        }

        await NormalizeResumeStateAsync(resume, cancellationToken);

        if (IsLockedDraftState(resume.Status))
        {
            throw new InvalidOperationException("Cannot edit an approved draft.");
        }

        if (!_jsonValidator.TryValidate(request.EditedResumeJson, out var failureReason))
        {
            throw new InvalidOperationException(failureReason ?? "Edited resume JSON is invalid.");
        }

        resume.EditedResumeJson = request.EditedResumeJson;
        resume.Status = ResumeDraftStatus.DraftReady;
        resume.FailedReason = null;
        resume.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Resume draft edited for user {UserId} and resume {ResumeId}", userId, resume.Id);

        return MapToDetailDto(resume);
    }

    public async Task<ApproveDraftResponse?> ApproveDraftAsync(string userId, int id, ApproveDraftRequest request, CancellationToken cancellationToken = default)
    {
        var resume = await _db.Resumes.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);

        if (resume is null)
        {
            return null;
        }

        await NormalizeResumeStateAsync(resume, cancellationToken);

        if (IsLockedDraftState(resume.Status))
        {
            throw new InvalidOperationException("Draft is already approved.");
        }

        if (!_jsonValidator.TryValidate(request.FinalResumeJson, out var failureReason))
        {
            throw new InvalidOperationException(failureReason ?? "Final resume JSON is invalid.");
        }

        var obsoletePdfBlob = resume.PdfBlobPath;
        resume.ApprovedJson = request.FinalResumeJson;
        resume.Status = ResumeDraftStatus.Approved;
        resume.ApprovedAt = DateTime.UtcNow;
        resume.PdfBlobPath = null;
        resume.PdfGeneratedAt = null;
        resume.PdfFailureReason = null;
        resume.FailedReason = null;
        resume.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Resume draft approved for user {UserId} and resume {ResumeId}", userId, resume.Id);

        if (!string.IsNullOrWhiteSpace(obsoletePdfBlob))
        {
            await _blobStorageService.DeleteResumePdfAsync(obsoletePdfBlob, cancellationToken);
        }

        return new ApproveDraftResponse
        {
            Id = resume.Id,
            Status = resume.Status,
            ApprovedAt = resume.ApprovedAt.Value,
            TargetCompany = resume.TargetCompany
        };
    }

    public async Task<ResumeDetailDto?> GeneratePdfAsync(string userId, int id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var resume = await _db.Resumes.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);

        if (resume is null)
        {
            return null;
        }

        await NormalizeResumeStateAsync(resume, cancellationToken);

        if (resume.Status == ResumeDraftStatus.PdfReady && !string.IsNullOrWhiteSpace(resume.PdfBlobPath) && await _blobStorageService.ResumePdfExistsAsync(resume.PdfBlobPath, cancellationToken))
        {
            stopwatch.Stop();
            _metrics.RecordResumePdfGeneration("success", stopwatch.Elapsed.TotalMilliseconds, userId);
            _logger.LogInformation("Resume PDF generation replayed idempotently for user {UserId} and resume {ResumeId}", userId, resume.Id);
            return MapToDetailDto(resume);
        }

        if (resume.Status is not (ResumeDraftStatus.Approved or ResumeDraftStatus.PdfFailed or ResumeDraftStatus.PdfReady))
        {
            throw new InvalidOperationException("PDF generation is only allowed for approved resumes.");
        }

        if (string.IsNullOrWhiteSpace(resume.ApprovedJson))
        {
            throw new InvalidOperationException("Approved resume content is missing.");
        }

        if (!_jsonValidator.TryValidate(resume.ApprovedJson, out var failureReason))
        {
            throw new InvalidOperationException(failureReason ?? "Approved resume JSON is invalid.");
        }

        var previousBlob = resume.PdfBlobPath;
        resume.Status = ResumeDraftStatus.PdfGenerating;
        resume.PdfFailureReason = null;
        resume.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var pdfBytes = _pdfRenderer.RenderResumePdf(resume.TargetCompany, resume.ApprovedJson);

            await using var stream = new MemoryStream(pdfBytes);
            var blobPath = await _blobStorageService.UploadResumePdfAsync(
                resume.UserId,
                resume.Id,
                stream,
                cancellationToken);

            resume.PdfBlobPath = blobPath;
            resume.PdfGeneratedAt = DateTime.UtcNow;
            resume.Status = ResumeDraftStatus.PdfReady;
            resume.PdfFailureReason = null;
            _logger.LogInformation("Resume PDF generated for user {UserId} and resume {ResumeId}", userId, resume.Id);

            if (!string.IsNullOrWhiteSpace(previousBlob) && !string.Equals(previousBlob, blobPath, StringComparison.OrdinalIgnoreCase))
            {
                await _blobStorageService.DeleteResumePdfAsync(previousBlob, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            resume.Status = ResumeDraftStatus.PdfFailed;
            resume.PdfFailureReason = Truncate(ex.Message);
            _logger.LogWarning(ex, "Resume PDF generation failed for user {UserId} and resume {ResumeId}", userId, resume.Id);
        }

        resume.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();
        _metrics.RecordResumePdfGeneration(
            resume.Status == ResumeDraftStatus.PdfReady ? "success" : "failure",
            stopwatch.Elapsed.TotalMilliseconds,
            userId);

        return MapToDetailDto(resume);
    }

    public async Task<ResumePdfDownloadResult?> GetPdfAsync(string userId, int id, CancellationToken cancellationToken = default)
    {
        var resume = await _db.Resumes.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);

        if (resume is null)
        {
            return null;
        }

        await NormalizeResumeStateAsync(resume, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        if (resume.Status != ResumeDraftStatus.PdfReady || string.IsNullOrWhiteSpace(resume.PdfBlobPath))
        {
            return null;
        }

        var pdf = await _blobStorageService.DownloadResumePdfAsync(resume.PdfBlobPath, cancellationToken);
        if (pdf is null)
        {
            resume.Status = ResumeDraftStatus.PdfFailed;
            resume.PdfFailureReason = "Generated PDF was missing from storage.";
            resume.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }

        return pdf;
    }

    public async Task<bool> DeleteDraftAsync(string userId, int id, CancellationToken cancellationToken = default)
    {
        var resume = await _db.Resumes.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);
        if (resume is null) return false;

        if (!string.IsNullOrWhiteSpace(resume.PdfBlobPath))
        {
            await _blobStorageService.DeleteResumePdfAsync(resume.PdfBlobPath, cancellationToken);
        }

        _db.Resumes.Remove(resume);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Resume {ResumeId} deleted for user {UserId}", id, userId);
        return true;
    }

    private async Task<bool> NormalizeResumeStateAsync(Resume resume, CancellationToken cancellationToken)
    {
        var changed = false;
        var now = DateTime.UtcNow;

        if (resume.Status is ResumeDraftStatus.Pending && !string.IsNullOrWhiteSpace(resume.GeneratedResumeJson))
        {
            resume.Status = ResumeDraftStatus.Generated;
            resume.FailedReason = null;
            changed = true;
        }

        if (resume.Status is ResumeDraftStatus.Pending && now - resume.CreatedAt >= StaleDraftAge)
        {
            resume.Status = ResumeDraftStatus.DraftFailed;
            resume.FailedReason = "Draft generation timed out before completion.";
            changed = true;
        }

        if (resume.Status is ResumeDraftStatus.DraftFailed && !string.IsNullOrWhiteSpace(resume.GeneratedResumeJson))
        {
            resume.Status = ResumeDraftStatus.Generated;
            resume.FailedReason = null;
            changed = true;
        }

        if (resume.Status is ResumeDraftStatus.PdfGenerating)
        {
            if (!string.IsNullOrWhiteSpace(resume.PdfBlobPath) && await _blobStorageService.ResumePdfExistsAsync(resume.PdfBlobPath, cancellationToken))
            {
                resume.Status = ResumeDraftStatus.PdfReady;
                resume.PdfFailureReason = null;
                changed = true;
            }
            else
            {
                resume.Status = ResumeDraftStatus.PdfFailed;
                resume.PdfFailureReason = "PDF generation did not complete.";
                changed = true;
            }
        }

        if (resume.Status is ResumeDraftStatus.PdfReady && !string.IsNullOrWhiteSpace(resume.PdfBlobPath) && !await _blobStorageService.ResumePdfExistsAsync(resume.PdfBlobPath, cancellationToken))
        {
            resume.Status = ResumeDraftStatus.PdfFailed;
            resume.PdfFailureReason = "Generated PDF was missing from storage.";
            changed = true;
        }

        return changed;
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        string operation,
        string userId,
        int resumeId,
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMilliseconds(200);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxTransientAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < MaxTransientAttempts && IsTransient(ex, cancellationToken))
            {
                lastException = ex;
                _logger.LogWarning(ex, "Transient failure during {Operation} for user {UserId} and resume {ResumeId}; retrying attempt {Attempt}", operation, userId, resumeId, attempt + 1);
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }

        throw lastException ?? new InvalidOperationException($"{operation} failed.");
    }

    private static bool IsTransient(Exception ex, CancellationToken cancellationToken)
        => ex switch
        {
            OperationCanceledException when cancellationToken.IsCancellationRequested => false,
            TaskCanceledException when cancellationToken.IsCancellationRequested => false,
            _ when ex is HttpRequestException or TaskCanceledException or TimeoutException => true,
            _ => false
        };

    private static bool IsLockedDraftState(ResumeDraftStatus status)
        => status is ResumeDraftStatus.Approved
            or ResumeDraftStatus.PdfGenerating
            or ResumeDraftStatus.PdfReady
            or ResumeDraftStatus.PdfFailed;

    private static string Truncate(string value, int maxLength = 2000)
        => value.Length <= maxLength ? value : value[..maxLength];

    private static ResumeDraftResponse MapToDraftResponse(Resume resume) => new()
    {
        Id = resume.Id,
        Status = resume.Status,
        TargetCompany = resume.TargetCompany,
        FailedReason = resume.FailedReason,
        CreatedAt = resume.CreatedAt,
        UpdatedAt = resume.UpdatedAt
    };

    private static readonly Expression<Func<Resume, ResumeDetailDto>> MapToDetailDtoProjection = resume => new ResumeDetailDto
    {
        Id = resume.Id,
        Status = resume.Status,
        TargetCompany = resume.TargetCompany,
        GenerationRequestJson = resume.GenerationRequestJson,
        GeneratedResumeJson = resume.GeneratedResumeJson,
        EditedResumeJson = resume.EditedResumeJson,
        ApprovedJson = resume.ApprovedJson,
        HasPdf = resume.PdfBlobPath != null && resume.PdfBlobPath != string.Empty,
        PdfGeneratedAt = resume.PdfGeneratedAt,
        PdfFailureReason = resume.PdfFailureReason,
        FailedReason = resume.FailedReason,
        CreatedAt = resume.CreatedAt,
        UpdatedAt = resume.UpdatedAt,
        ApprovedAt = resume.ApprovedAt
    };

    private static ResumeDetailDto MapToDetailDto(Resume resume) => new()
    {
        Id = resume.Id,
        Status = resume.Status,
        TargetCompany = resume.TargetCompany,
        GenerationRequestJson = resume.GenerationRequestJson,
        GeneratedResumeJson = resume.GeneratedResumeJson,
        EditedResumeJson = resume.EditedResumeJson,
        ApprovedJson = resume.ApprovedJson,
        HasPdf = !string.IsNullOrWhiteSpace(resume.PdfBlobPath),
        PdfGeneratedAt = resume.PdfGeneratedAt,
        PdfFailureReason = resume.PdfFailureReason,
        FailedReason = resume.FailedReason,
        CreatedAt = resume.CreatedAt,
        UpdatedAt = resume.UpdatedAt,
        ApprovedAt = resume.ApprovedAt
    };
}
