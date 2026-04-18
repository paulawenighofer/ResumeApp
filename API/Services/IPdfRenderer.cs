namespace API.Services;

public interface IPdfRenderer
{
    byte[] RenderResumePdf(string targetCompany, string resumeJson);
}
