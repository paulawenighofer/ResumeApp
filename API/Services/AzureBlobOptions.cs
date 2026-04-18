namespace API.Services;

public sealed class AzureBlobOptions
{
    public const string SectionName = "AzureBlob";

    public string ConnectionString { get; set; } = string.Empty;

    public string ProfileImagesContainer { get; set; } = string.Empty;

    public string? ProfileImagesBasePath { get; set; }
}
