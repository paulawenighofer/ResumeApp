using API.Data;
using API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;

namespace Test.Integration.Fixtures;

public class PostgresApiFactory : WebApplicationFactory<Program>
{
    private readonly bool _useProductionRateLimits;
    private readonly bool _overrideEmailService;
    private readonly bool _emailOtpDeliveryEnabled;
    private Respawner? _respawner;

    public PostgresApiFactory() : this(useProductionRateLimits: false)
    {
    }

    internal PostgresApiFactory(
        bool useProductionRateLimits,
        bool overrideEmailService = true,
        bool emailOtpDeliveryEnabled = true)
    {
        _useProductionRateLimits = useProductionRateLimits;
        _overrideEmailService = overrideEmailService;
        _emailOtpDeliveryEnabled = emailOtpDeliveryEnabled;
    }

    public FakeEmailService EmailService { get; } = new();
    public FakeAiResumeGenerationClient AiResumeGenerationClient { get; } = new();
    public FakeBlobStorageService BlobStorageService { get; } = new();
    public FakePdfRenderer PdfRenderer { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var integrationDbConnection =
                Environment.GetEnvironmentVariable("INTEGRATION_TEST_DB_CONNECTION")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? throw new InvalidOperationException("Integration tests require INTEGRATION_TEST_DB_CONNECTION or ConnectionStrings__DefaultConnection.");

            var testConfig = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = integrationDbConnection,
                ["Jwt:Key"] = "TestSecretKeyThatIsLongEnoughForHS256!!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpirationInMinutes"] = "60",
                ["RateLimiting:Enabled"] = _useProductionRateLimits.ToString(),
                ["FeatureFlags:EmailOtpDelivery"] = _emailOtpDeliveryEnabled.ToString(),
                ["AiService:BaseUrl"] = "https://ai.test.local/generate",
                ["AiService:ApiKey"] = "test-key",
                ["AiService:Model"] = "test-model",
                ["AiService:TimeoutSeconds"] = "60",
                ["ResumeDraftProcessing:ProcessInBackground"] = "false",
                ["AzureBlob:ConnectionString"] = "UseDevelopmentStorage=true",
                ["AzureBlob:ProfileImagesContainer"] = "profile-images",
                ["AzureBlob:ProfileImagesBasePath"] = "",
                ["AzureBlob:ResumesContainer"] = "resumes",
                ["AzureBlob:ResumesBasePath"] = "",
            };

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            if (_overrideEmailService)
            {
                services.RemoveAll(typeof(IEmailService));
                services.AddScoped<IEmailService>(_ => EmailService);
            }

            services.RemoveAll(typeof(IAiResumeGenerationClient));
            services.AddScoped<IAiResumeGenerationClient>(_ => AiResumeGenerationClient);

            services.RemoveAll(typeof(IBlobStorageService));
            services.AddSingleton<IBlobStorageService>(BlobStorageService);

            services.RemoveAll(typeof(IPdfRenderer));
            services.AddSingleton<IPdfRenderer>(PdfRenderer);
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await EnsureRespawnerInitializedAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connectionString = db.Database.GetDbConnection().ConnectionString;

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await _respawner!.ResetAsync(conn);
    }

    private async Task EnsureRespawnerInitializedAsync()
    {
        if (_respawner != null)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connectionString = db.Database.GetDbConnection().ConnectionString;

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory"],
        });
    }
}

public class ApiFactory : PostgresApiFactory
{
    public ApiFactory() : base(useProductionRateLimits: false)
    {
    }

    internal ApiFactory(
        bool useProductionRateLimits,
        bool overrideEmailService = true,
        bool emailOtpDeliveryEnabled = true)
        : base(useProductionRateLimits, overrideEmailService, emailOtpDeliveryEnabled)
    {
    }
}
