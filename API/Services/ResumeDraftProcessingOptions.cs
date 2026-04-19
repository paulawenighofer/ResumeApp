namespace API.Services;

public class ResumeDraftProcessingOptions
{
    public const string SectionName = "ResumeDraftProcessing";

    public bool ProcessInBackground { get; set; } = true;
}
