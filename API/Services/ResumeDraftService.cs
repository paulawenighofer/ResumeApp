using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTO;
using Shared.Models;
using System.Linq.Expressions;

namespace API.Services;

public class ResumeDraftService : IResumeDraftService
{
    private readonly AppDbContext _db;
    private readonly IResumeProfileAssembler _profileAssembler;
    private readonly IAiResumeGenerationClient _aiClient;
    private readonly IResumeJsonValidator _jsonValidator;
    private readonly IPdfRenderer _pdfRenderer;
    private readonly IBlobStorageService _blobStorageService;

    public ResumeDraftService(
        AppDbContext db,
        IResumeProfileAssembler profileAssembler,
        IAiResumeGenerationClient aiClient,
        IResumeJsonValidator jsonValidator,
        IPdfRenderer pdfRenderer,
        IBlobStorageService blobStorageService)
    {
        _db = db;
        _profileAssembler = profileAssembler;
        _aiClient = aiClient;
        _jsonValidator = jsonValidator;
        _pdfRenderer = pdfRenderer;
        _blobStorageService = blobStorageService;
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

        try
        {
            var generatedJson = await _aiClient.GenerateResumeJsonAsync(assembled.Prompt, cancellationToken);

            if (!_jsonValidator.TryValidate(generatedJson, out var failureReason))
            {
                throw new InvalidOperationException(failureReason ?? "AI output validation failed.");
            }

            draft.GeneratedResumeJson = generatedJson;
            draft.Status = ResumeDraftStatus.Generated;
            draft.FailedReason = null;
        }
        catch (Exception ex)
        {
            draft.Status = ResumeDraftStatus.Failed;
            draft.FailedReason = ex.Message.Length > 2000
                ? ex.Message[..2000]
                : ex.Message;
        }

        draft.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDraftResponse(draft);
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
        return await _db.Resumes
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Id == id)
            .Select(MapToDetailDtoProjection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ResumeDetailDto?> SaveDraftEditAsync(string userId, int id, SaveDraftEditRequest request, CancellationToken cancellationToken = default)
    {
        var resume = await _db.Resumes.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);

        if (resume is null)
        {
            return null;
        }

        if (IsApprovedOrBeyond(resume.Status))
        {
            throw new InvalidOperationException("Cannot edit an approved draft.");
        }

        resume.EditedResumeJson = request.EditedResumeJson;
        resume.Status = ResumeDraftStatus.DraftReady;
        resume.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(resume);
    }

    public async Task<ApproveDraftResponse?> ApproveDraftAsync(string userId, int id, ApproveDraftRequest request, CancellationToken cancellationToken = default)
    {
        var resume = await _db.Resumes.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);

        if (resume is null)
        {
            return null;
        }

        if (IsApprovedOrBeyond(resume.Status))
        {
            throw new InvalidOperationException("Draft is already approved.");
        }

        resume.ApprovedJson = request.FinalResumeJson;
        resume.Status = ResumeDraftStatus.Approved;
        resume.ApprovedAt = DateTime.UtcNow;
        resume.PdfBlobPath = null;
        resume.PdfGeneratedAt = null;
        resume.FailedReason = null;
        resume.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

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
        var resume = await _db.Resumes.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);

        if (resume is null)
        {
            return null;
        }

        if (resume.Status is not (ResumeDraftStatus.Approved or ResumeDraftStatus.PdfFailed))
        {
            throw new InvalidOperationException("PDF generation is only allowed for approved resumes.");
        }

        if (string.IsNullOrWhiteSpace(resume.ApprovedJson))
        {
            throw new InvalidOperationException("Approved resume content is missing.");
        }

        resume.Status = ResumeDraftStatus.PdfGenerating;
        resume.FailedReason = null;
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
            resume.FailedReason = null;
        }
        catch (Exception ex)
        {
            resume.Status = ResumeDraftStatus.PdfFailed;
            resume.FailedReason = ex.Message.Length > 2000
                ? ex.Message[..2000]
                : ex.Message;
        }

        resume.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(resume);
    }

    public async Task<ResumePdfDownloadResult?> GetPdfAsync(string userId, int id, CancellationToken cancellationToken = default)
    {
        var resume = await _db.Resumes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);

        if (resume is null)
        {
            return null;
        }

        if (resume.Status != ResumeDraftStatus.PdfReady || string.IsNullOrWhiteSpace(resume.PdfBlobPath))
        {
            throw new InvalidOperationException("PDF is not ready.");
        }

        var stream = await _blobStorageService.DownloadResumePdfAsync(resume.PdfBlobPath, cancellationToken);
        if (stream is null)
        {
            throw new InvalidOperationException("PDF file not found in storage.");
        }

        var fileName = $"resume-{resume.Id}.pdf";
        return new ResumePdfDownloadResult(stream, fileName);
    }

    private static bool IsApprovedOrBeyond(ResumeDraftStatus status)
        => status is ResumeDraftStatus.Approved
            or ResumeDraftStatus.PdfGenerating
            or ResumeDraftStatus.PdfReady
            or ResumeDraftStatus.PdfFailed;

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
        FailedReason = resume.FailedReason,
        CreatedAt = resume.CreatedAt,
        UpdatedAt = resume.UpdatedAt,
        ApprovedAt = resume.ApprovedAt
    };
}
