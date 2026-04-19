using API.Data;
using API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Test.Integration.Fixtures;

/// <summary>
/// Spins up the full ASP.NET Core pipeline for integration tests.
/// By default it swaps PostgreSQL for an in-memory database, but it can also
/// target a disposable real PostgreSQL database when the CI environment enables it.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly bool _useProductionRateLimits;
    private readonly bool _useRealPostgres;
    private string? _adminConnectionString;
    private string? _testDatabaseName;
    private string? _testDatabaseConnectionString;
    private bool _databaseDropped;

    public ApiFactory() : this(useProductionRateLimits: false)
    {
    }

    internal ApiFactory(bool useProductionRateLimits)
    {
        _useProductionRateLimits = useProductionRateLimits;
        _useRealPostgres = string.Equals(
            Environment.GetEnvironmentVariable("INTEGRATION_TEST_USE_REAL_POSTGRES"),
            "true",
            StringComparison.OrdinalIgnoreCase);
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

            if (_useRealPostgres)
            {
                EnsureRealPostgresDatabaseCreated();
                testConfig["ConnectionStrings:DefaultConnection"] = _testDatabaseConnectionString;
            }

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            if (_useRealPostgres)
            {
                ReplaceEmailService(services);

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
                return;
            }

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

            ReplaceEmailService(services);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            DropRealPostgresDatabase();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        DropRealPostgresDatabase();
    }

    private void ReplaceEmailService(IServiceCollection services)
    {
        var emailHits = services.Where(d => d.ServiceType == typeof(IEmailService)).ToList();
        foreach (var d in emailHits) services.Remove(d);
        services.AddScoped<IEmailService>(_ => EmailService);
    }

    private void EnsureRealPostgresDatabaseCreated()
    {
        if (_testDatabaseConnectionString is not null)
        {
            return;
        }

        var baseConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            throw new InvalidOperationException(
                "INTEGRATION_TEST_USE_REAL_POSTGRES is enabled, but ConnectionStrings__DefaultConnection is missing.");
        }

        var appBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString);
        var baseDatabaseName = string.IsNullOrWhiteSpace(appBuilder.Database) ? "resumeapp" : appBuilder.Database;
        _testDatabaseName = $"{baseDatabaseName}_it_{Guid.NewGuid():N}";
        appBuilder.Database = _testDatabaseName;
        _testDatabaseConnectionString = appBuilder.ConnectionString;

        var adminBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = "postgres"
        };
        _adminConnectionString = adminBuilder.ConnectionString;

        using var adminConnection = new NpgsqlConnection(_adminConnectionString);
        adminConnection.Open();

        using var createCommand = adminConnection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE \"{_testDatabaseName}\"";
        createCommand.ExecuteNonQuery();
    }

    private void DropRealPostgresDatabase()
    {
        if (_databaseDropped || !_useRealPostgres || string.IsNullOrWhiteSpace(_adminConnectionString) || string.IsNullOrWhiteSpace(_testDatabaseName))
        {
            return;
        }

        _databaseDropped = true;

        using var adminConnection = new NpgsqlConnection(_adminConnectionString);
        adminConnection.Open();

        using (var terminateCommand = adminConnection.CreateCommand())
        {
            terminateCommand.CommandText = """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @databaseName AND pid <> pg_backend_pid()
                """;
            terminateCommand.Parameters.AddWithValue("databaseName", _testDatabaseName);
            terminateCommand.ExecuteNonQuery();
        }

        using var dropCommand = adminConnection.CreateCommand();
        dropCommand.CommandText = $"DROP DATABASE IF EXISTS \"{_testDatabaseName}\"";
        dropCommand.ExecuteNonQuery();
    }
}
