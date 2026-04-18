using API.Services;
using System.Text;

namespace Test.Integration.Fixtures;

public sealed class FakePdfRenderer : IPdfRenderer
{
    public bool ShouldFail { get; set; }

    public byte[] RenderResumePdf(string targetCompany, string resumeJson)
    {
        if (ShouldFail)
        {
            throw new InvalidOperationException("Rendering failed in test renderer.");
        }

        return Encoding.UTF8.GetBytes($"PDF:{targetCompany}:{resumeJson}");
    }
}
