namespace ResumeApp.Services;

public interface IPdfService
{
    Task<PdfExportResult> CreatePdfFromJsonAsync(string jsonContent, string fileNameWithoutExtension, CancellationToken cancellationToken = default);
}
