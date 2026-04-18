namespace API.Services;

public sealed class AzureBlobOptions
{
    public const string SectionName = "AzureBlob";

    public string ConnectionString { get; init; } = string.Empty;

    public string ProfileImagesContainer { get; init; } = string.Empty;

    public string? ProfileImagesBasePath { get; init; }
}
