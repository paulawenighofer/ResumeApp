namespace API.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string folder, HttpRequest request)
    {
        var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;

        var uploadFolder = Path.Combine(webRoot, "uploads", folder);
        Directory.CreateDirectory(uploadFolder);

        var finalPath = Path.Combine(uploadFolder, fileName);
        await using var fileStream = File.Create(finalPath);
        await stream.CopyToAsync(fileStream);

        var publicPath = $"/uploads/{folder}/{fileName}";
        return $"{request.Scheme}://{request.Host}{publicPath}";
    }
}
