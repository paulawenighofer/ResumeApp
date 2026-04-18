namespace API.Services;

public interface IResumeJsonValidator
{
    bool TryValidate(string generatedResumeJson, out string? failureReason);
}
