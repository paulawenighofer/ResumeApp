namespace ResumeApp.Services;

public interface IApiService
{
    Task<bool> UploadProjectImagesAsync(string projectId, IReadOnlyCollection<string> imagePaths);
    Task<string?> UploadProfileImageAsync(string imagePath);
    Task<string?> UploadResumeFileAsync(string resumeId, string filePath);
}
