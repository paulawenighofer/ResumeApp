using Microsoft.Extensions.Options;

namespace API.Services;

public class ResumeDraftGenerationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IResumeDraftGenerationQueue _queue;
    private readonly IOptions<ResumeDraftProcessingOptions> _options;
    private readonly ILogger<ResumeDraftGenerationWorker> _logger;

    public ResumeDraftGenerationWorker(
        IServiceScopeFactory scopeFactory,
        IResumeDraftGenerationQueue queue,
        IOptions<ResumeDraftProcessingOptions> options,
        ILogger<ResumeDraftGenerationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.ProcessInBackground)
        {
            _logger.LogInformation("Resume draft background worker is disabled by configuration.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _queue.DequeueAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ResumeDraftService>();
                await service.ProcessDraftGenerationAsync(workItem.UserId, workItem.ResumeId, workItem.Prompt, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while processing resume generation queue item.");
            }
        }
    }
}
