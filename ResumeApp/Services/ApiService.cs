using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ResumeApp.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> UploadProjectImagesAsync(string projectId, IReadOnlyCollection<string> imagePaths)
    {
        if (imagePaths.Count == 0)
        {
            return true;
        }

        var content = BuildMultipartContent(imagePaths, "files");
        var response = await SendAsync(HttpMethod.Post, $"api/projects/{projectId}/images", content);
        return response?.IsSuccessStatusCode == true;
    }

    public async Task<string?> UploadProfileImageAsync(string imagePath)
    {
        var content = BuildMultipartContent([imagePath], "file");
        var response = await SendAsync(HttpMethod.Post, "api/profile/image", content);
        if (response is null || !response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<ImageUploadResponse>();
        return result?.ImageUrl;
    }

    public async Task<string?> UploadResumeFileAsync(string resumeId, string filePath)
    {
        var content = BuildMultipartContent([filePath], "file");
        var response = await SendAsync(HttpMethod.Post, $"api/resumes/{resumeId}/file", content);
        if (response is null || !response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<FileUploadResponse>();
        return result?.FileUrl;
    }

    private async Task<HttpResponseMessage?> SendAsync(HttpMethod method, string url, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = content
        };

        var token = await SecureStorage.GetAsync("auth_token");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        try
        {
            return await _httpClient.SendAsync(request);
        }
        catch
        {
            return null;
        }
    }

    private static MultipartFormDataContent BuildMultipartContent(IEnumerable<string> filePaths, string fieldName)
    {
        var content = new MultipartFormDataContent();

        foreach (var filePath in filePaths.Where(File.Exists))
        {
            var fileContent = new StreamContent(File.OpenRead(filePath));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(filePath));
            content.Add(fileContent, fieldName, Path.GetFileName(filePath));
        }

        return content;
    }

    private static string GetMimeType(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _ => "image/jpeg"
    };

    private sealed class ImageUploadResponse
    {
        public string? ImageUrl { get; set; }
    }

    private sealed class FileUploadResponse
    {
        public string? FileUrl { get; set; }
    }
}
