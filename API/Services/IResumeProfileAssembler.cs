using Shared.DTO;

namespace API.Services;

public interface IResumeProfileAssembler
{
    Task<ResumeGenerationPayload> AssembleAsync(string userId, CreateResumeDraftRequest request, CancellationToken cancellationToken = default);
}
