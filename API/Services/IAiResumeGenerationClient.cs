namespace API.Services;

public interface IAiResumeGenerationClient
{
    Task<string> GenerateResumeJsonAsync(string prompt, CancellationToken cancellationToken = default);
}
