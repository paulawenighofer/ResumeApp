using Shared.DTO;

namespace API.Services;

public interface IResumeDraftService
{
    Task<ResumeDraftResponse> CreateDraftAsync(string userId, CreateResumeDraftRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResumeListItemDto>> ListDraftsAsync(string userId, CancellationToken cancellationToken = default);
    Task<ResumeDetailDto?> GetDraftAsync(string userId, int id, CancellationToken cancellationToken = default);
    Task<ResumeDetailDto?> SaveDraftEditAsync(string userId, int id, SaveDraftEditRequest request, CancellationToken cancellationToken = default);
    Task<ApproveDraftResponse?> ApproveDraftAsync(string userId, int id, ApproveDraftRequest request, CancellationToken cancellationToken = default);
}
