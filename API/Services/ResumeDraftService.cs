using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTO;
using Shared.Models;

namespace API.Services;

public class ResumeDraftService : IResumeDraftService
{
    private readonly AppDbContext _db;
    private readonly IResumeProfileAssembler _profileAssembler;
    private readonly IAiResumeGenerationClient _aiClient;
    private readonly IResumeJsonValidator _jsonValidator;

    public ResumeDraftService(
        AppDbContext db,
        IResumeProfileAssembler profileAssembler,
        IAiResumeGenerationClient aiClient,
        IResumeJsonValidator jsonValidator)
    {
        _db = db;
        _profileAssembler = profileAssembler;
        _aiClient = aiClient;
        _jsonValidator = jsonValidator;
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
            .Select(x => new ResumeDetailDto
            {
                Id = x.Id,
                Status = x.Status,
                TargetCompany = x.TargetCompany,
                GenerationRequestJson = x.GenerationRequestJson,
                GeneratedResumeJson = x.GeneratedResumeJson,
                EditedResumeJson = x.EditedResumeJson,
                FailedReason = x.FailedReason,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static ResumeDraftResponse MapToDraftResponse(Resume resume) => new()
    {
        Id = resume.Id,
        Status = resume.Status,
        TargetCompany = resume.TargetCompany,
        FailedReason = resume.FailedReason,
        CreatedAt = resume.CreatedAt,
        UpdatedAt = resume.UpdatedAt
    };
}
