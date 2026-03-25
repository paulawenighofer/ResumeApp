using API.Data;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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


// =============================================
// SECTION 3: AUTHENTICATION 
// =============================================
var authBuilder = builder.Services.AddAuthentication(options =>
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

builder.Services.AddControllers();

var app = builder.Build();

// Auto-apply migrations on startup so the DB is always in sync
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
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
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.MapGet("/api/", () => "Hello World");

app.Run();
