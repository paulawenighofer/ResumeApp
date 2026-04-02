using API.Data;
using API.Middleware;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Models;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var telemetryServiceName = "ResumeApp.API";
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? string.Empty;
var hasOtlpEndpoint = !string.IsNullOrWhiteSpace(otlpEndpoint);

// Configure logging to send to OpenTelemetry
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    // Add resource with service name
    options.SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(serviceName: telemetryServiceName, serviceVersion: "1.0.0"));

    if (!hasOtlpEndpoint)
    {
        return;
    }

    options.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri(otlpEndpoint);
        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
    });
});
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<InMemoryResumeStore>();

// =============================================
// SECTION 1: DATABASE
// =============================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// =============================================
// SECTION 2: IDENTITY (User Management)
// =============================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // If the password doesn't meet these rules, registration fails
    // with a descriptive error message.
    options.Password.RequireDigit = true;           // Must have a number
    options.Password.RequiredLength = 8;            // At least 8 characters
    options.Password.RequireNonAlphanumeric = false; // No special chars needed (keep it simple for MVP)
    options.Password.RequireUppercase = true;        // Must have uppercase
    options.Password.RequireLowercase = true;        // Must have lowercase

    // Lockout — if someone tries 5 wrong passwords, lock the account for 5 minutes.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // Every email must be unique — no two users can register with the same email
    options.User.RequireUniqueEmail = true;
})
// This line connects Identity to our database context.
.AddEntityFrameworkStores<AppDbContext>()
// Adds token providers for things like email confirmation and password reset.
.AddDefaultTokenProviders();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
// Configure JWT validation
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,           // Check the Issuer claim matches
        ValidateAudience = true,         // Check the Audience claim matches
        ValidateLifetime = true,         // Check the token hasn't expired
        ValidateIssuerSigningKey = true,  // Verify the signature is genuine
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        // This is the key used to verify token signatures.
        // It must match the key used to CREATE tokens (in TokenService).
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
    // Validate SecurityStamp on every authenticated request.
    // When logout-all is called, UpdateSecurityStampAsync rotates the stamp,
    // so all tokens issued before that moment fail this check immediately.
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userManager = context.HttpContext.RequestServices
                .GetRequiredService<UserManager<ApplicationUser>>();

            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var stampClaim = context.Principal?.FindFirstValue("security_stamp");

            if (string.IsNullOrEmpty(userId) || stampClaim == null)
            {
                context.Fail("Invalid token claims.");
                return;
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null || user.SecurityStamp != stampClaim)
                context.Fail("Token has been revoked.");
        }
    };
});

// =============================================
// SECTION 4: OTHER SERVICES
// =============================================
// creating new HttpClient instances manually (for better performance in http requests)
builder.Services.AddHttpClient();
// Register our custom TokenService so we can inject it into controllers
builder.Services.AddScoped<TokenService>();
// Register our social auth service
builder.Services.AddScoped<SocialAuthService>();
// Register the email service — sends OTP codes and password-reset links via SMTP
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// =============================================
// SECTION 5: RATE LIMITING
// =============================================
// Protects OTP endpoints against brute-force attacks.
// "otp-verify"  — applied to verify-otp and reset-password: 5 attempts per 15 min per IP.
//   With 1,000,000 possible codes an attacker would need ~200,000 windows to brute-force
//   a single code, making automated attacks impractical within the 10-minute OTP window.
// "otp-send"    — applied to forgot-password and resend-otp: 3 sends per 10 min per IP.
//   Prevents email flooding / SMS-bombing style abuse.
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("otp-verify", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(15);
        opt.SegmentsPerWindow = 3;
        opt.QueueLimit = 0;
    });

    options.AddSlidingWindowLimiter("otp-send", opt =>
    {
        opt.PermitLimit = 3;
        opt.Window = TimeSpan.FromMinutes(10);
        opt.SegmentsPerWindow = 2;
        opt.QueueLimit = 0;
    });

    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.StatusCode = 429;
        await ctx.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many attempts. Please try again later." }, token);
    };
});

builder.Services.AddControllers();

// =============================================
// SECTION 6: OPENTELEMETRY
// =============================================
builder.Services.AddSingleton<ApiMetrics>();

// Configure OpenTelemetry tracing and metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: telemetryServiceName, serviceVersion: "1.0.0"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation();

        if (hasOtlpEndpoint)
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        if (hasOtlpEndpoint)
        {
            metrics.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
        }
    });


var app = builder.Build();

// Auto-apply migrations on startup so the DB is always in sync
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database migration skipped. Check PostgreSQL availability and connection settings.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.MapGet("/api/", () => "Hello World!");

app.Run();

public partial class Program { }
