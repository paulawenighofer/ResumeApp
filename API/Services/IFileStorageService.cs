namespace API.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream stream, string fileName, string folder, HttpRequest request);
}
