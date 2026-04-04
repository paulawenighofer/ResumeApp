namespace API.Services;

public sealed class DemoTelemetryService : BackgroundService
{
    private static readonly string[] Providers = ["google", "linkedin", "github"];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ApiMetrics _metrics;
    private readonly ILogger<DemoTelemetryService> _logger;

    public DemoTelemetryService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ApiMetrics metrics,
        ILogger<DemoTelemetryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _metrics = metrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue<bool>("Dashboard:EnableDemoTelemetry");
        if (!enabled)
        {
            _logger.LogInformation("Demo telemetry is disabled.");
            return;
        }

        var baseUrl = _configuration["Dashboard:InternalBaseUrl"] ?? "http://localhost:8080";
        var intervalSeconds = Math.Max(5, _configuration.GetValue("Dashboard:DemoTelemetryIntervalSeconds", 12));
        var httpClient = _httpClientFactory.CreateClient();

        _logger.LogInformation(
            "Demo telemetry is enabled. Synthetic traffic will target {BaseUrl} every {IntervalSeconds} seconds.",
            baseUrl,
            intervalSeconds);

        var iteration = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            iteration++;

            try
            {
                await httpClient.GetAsync($"{baseUrl.TrimEnd('/')}/api/", stoppingToken);
                await httpClient.GetAsync($"{baseUrl.TrimEnd('/')}/Dashboard", stoppingToken);

                EmitSyntheticMetrics(iteration);

                _logger.LogInformation(
                    "Demo telemetry heartbeat {Iteration} completed and synthetic observability data was emitted.",
                    iteration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Demo telemetry heartbeat failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private void EmitSyntheticMetrics(int iteration)
    {
        _metrics.LoginAttempts.Add(1, new KeyValuePair<string, object?>("result", "success"));

        if (iteration % 2 == 0)
        {
            _metrics.UserRegistrations.Add(1, new KeyValuePair<string, object?>("result", "success"));
            _metrics.OtpVerifications.Add(1, new KeyValuePair<string, object?>("result", "success"));
        }

        if (iteration % 3 == 0)
        {
            _metrics.PasswordResets.Add(1, new KeyValuePair<string, object?>("result", "requested"));
            _metrics.EducationsCreated.Add(1);
        }

        if (iteration % 4 == 0)
        {
            _metrics.ExperiencesCreated.Add(1);
            _metrics.SkillsCreated.Add(1);
        }

        if (iteration % 5 == 0)
        {
            var provider = Providers[iteration % Providers.Length];
            _metrics.SocialLogins.Add(1, new KeyValuePair<string, object?>("provider", provider));
            _metrics.ProjectsCreated.Add(1);
        }
    }
}
