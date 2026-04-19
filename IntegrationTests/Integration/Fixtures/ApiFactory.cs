using API.Data;
using API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Test.Integration.Fixtures;

// /// <summary>
// /// Spins up the full ASP.NET Core pipeline in-memory for integration tests.
// /// Replaces PostgreSQL with an in-memory database and the real email service
// /// with FakeEmailService so tests never hit external systems.
// /// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly bool _useProductionRateLimits;
    private readonly bool _overrideEmailService;
    private readonly bool _emailOtpDeliveryEnabled;

    public ApiFactory() : this(useProductionRateLimits: false)
    {
    }

    internal ApiFactory(
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
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestSecretKeyThatIsLongEnoughForHS256!!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpirationInMinutes"] = "60",
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
                ["RateLimiting:ResumePdfGeneration:PermitLimit"] = "1000000",
                ["RateLimiting:ResumePdfGeneration:WindowHours"] = "1",
            };

            if (!_useProductionRateLimits)
            {
                testConfig["RateLimiting:OtpVerify:PermitLimit"] = "1000000";
                testConfig["RateLimiting:OtpVerify:WindowMinutes"] = "1";
                testConfig["RateLimiting:OtpVerify:SegmentsPerWindow"] = "1";
                testConfig["RateLimiting:OtpSend:PermitLimit"] = "1000000";
                testConfig["RateLimiting:OtpSend:WindowMinutes"] = "1";
                testConfig["RateLimiting:OtpSend:SegmentsPerWindow"] = "1";
                testConfig["RateLimiting:ResumeGeneration:PermitLimit"] = "1000000";
                testConfig["RateLimiting:ResumeGeneration:WindowHours"] = "1";
                testConfig["RateLimiting:ResumePdfGeneration:PermitLimit"] = "1000000";
            }

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // EF Core 9 accumulates options via IDbContextOptionsConfiguration<T>.
            // If we only remove DbContextOptions<AppDbContext>, the Npgsql config
            // delegate still runs when the options are rebuilt, producing two providers.
            // Remove all three layers:
            //   1. The lazy option-config delegates (EF Core 9 internal interface)
            //   2. The cached DbContextOptions<T> and base DbContextOptions
            //   3. The AppDbContext scoped registration itself
            Type[] toStrip =
            [
                typeof(IDbContextOptionsConfiguration<AppDbContext>),
                typeof(DbContextOptions<AppDbContext>),
                typeof(DbContextOptions),
                typeof(AppDbContext),
            ];
            foreach (var type in toStrip) services.RemoveAll(type);

            // Register a fresh DbContext backed by a unique in-memory database.
            // The name is evaluated ONCE here (not inside the lambda) so that all
            // requests within a factory share the same in-memory store.
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase(dbName));

            if (_overrideEmailService)
            {
                // Replace the app-registered email service with our capturable fake.
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
}
