using System.Threading.Channels;

namespace API.Services;

public sealed record ResumeDraftGenerationWorkItem(string UserId, int ResumeId, string Prompt);

public interface IResumeDraftGenerationQueue
{
    ValueTask EnqueueAsync(ResumeDraftGenerationWorkItem workItem, CancellationToken cancellationToken = default);
    ValueTask<ResumeDraftGenerationWorkItem> DequeueAsync(CancellationToken cancellationToken);
}

public class ResumeDraftGenerationQueue : IResumeDraftGenerationQueue
{
    private readonly Channel<ResumeDraftGenerationWorkItem> _channel = Channel.CreateUnbounded<ResumeDraftGenerationWorkItem>();

    public ValueTask EnqueueAsync(ResumeDraftGenerationWorkItem workItem, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(workItem, cancellationToken);

    public ValueTask<ResumeDraftGenerationWorkItem> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);
}
