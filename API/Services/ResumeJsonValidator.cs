using System.Text.Json;

namespace API.Services;

public class ResumeJsonValidator : IResumeJsonValidator
{
    private const int MaxGeneratedJsonLength = 20000;

    private static readonly HashSet<string> AllowedTopLevelFields =
    [
        "user",
        "education",
        "experience",
        "skills",
        "projects",
        "certifications"
    ];

    public bool TryValidate(string generatedResumeJson, out string? failureReason)
    {
        failureReason = null;

        if (string.IsNullOrWhiteSpace(generatedResumeJson))
        {
            failureReason = "AI returned an empty response.";
            return false;
        }

        if (generatedResumeJson.Length > MaxGeneratedJsonLength)
        {
            failureReason = $"AI response exceeded the maximum allowed size of {MaxGeneratedJsonLength} characters.";
            return false;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(generatedResumeJson);
        }
        catch (JsonException ex)
        {
            failureReason = $"AI response is not valid JSON: {ex.Message}";
            return false;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                failureReason = "AI response must be a JSON object.";
                return false;
            }

            foreach (var property in root.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(property.Name) || !AllowedTopLevelFields.Contains(property.Name))
                {
                    failureReason = $"Unsupported top-level field '{property.Name}' in AI response.";
                    return false;
                }

                if (property.Name == "user" && property.Value.ValueKind != JsonValueKind.Object)
                {
                    failureReason = "Field 'user' must be a JSON object.";
                    return false;
                }

                if (property.Name != "user" && property.Value.ValueKind != JsonValueKind.Array)
                {
                    failureReason = $"Field '{property.Name}' must be a JSON array.";
                    return false;
                }
            }
        }

        return true;
    }
}
