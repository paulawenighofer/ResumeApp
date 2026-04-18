namespace Shared.Models;

public enum ResumeDraftStatus
{
    Pending = 0,
    Generated = 1,
    Failed = 2,
    DraftReady = 3,
    Approved = 4,
    PdfGenerating = 5,
    PdfReady = 6,
    PdfFailed = 7
}
