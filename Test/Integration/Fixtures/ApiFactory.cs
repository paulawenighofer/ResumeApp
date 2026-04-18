using API.Data;
using API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Test.Integration.Fixtures;

/// <summary>
/// Spins up the full ASP.NET Core pipeline in-memory for integration tests.
/// Replaces PostgreSQL with an in-memory database and the real email service
/// with FakeEmailService so tests never hit external systems.
/// </summary>
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
                ["AzureBlob:ConnectionString"] = "UseDevelopmentStorage=true",
                ["AzureBlob:ProfileImagesContainer"] = "profile-images",
                ["AzureBlob:ProfileImagesBasePath"] = "",
            };

            if (!_useProductionRateLimits)
            {
                testConfig["RateLimiting:OtpVerify:PermitLimit"] = "1000000";
                testConfig["RateLimiting:OtpVerify:WindowMinutes"] = "1";
                testConfig["RateLimiting:OtpVerify:SegmentsPerWindow"] = "1";
                testConfig["RateLimiting:OtpSend:PermitLimit"] = "1000000";
                testConfig["RateLimiting:OtpSend:WindowMinutes"] = "1";
                testConfig["RateLimiting:OtpSend:SegmentsPerWindow"] = "1";
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
            foreach (var type in toStrip)
            {
                var hits = services.Where(d => d.ServiceType == type).ToList();
                foreach (var d in hits) services.Remove(d);
            }

            // Register a fresh DbContext backed by a unique in-memory database.
            // The name is evaluated ONCE here (not inside the lambda) so that all
            // requests within a factory share the same in-memory store.
            var dbName = Guid.NewGuid().ToString();
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase(dbName));

            if (_overrideEmailService)
            {
                // Replace the app-registered email service with our capturable fake.
                var emailHits = services.Where(d => d.ServiceType == typeof(IEmailService)).ToList();
                foreach (var d in emailHits) services.Remove(d);
                services.AddScoped<IEmailService>(_ => EmailService);
            }
        });
    }
}
