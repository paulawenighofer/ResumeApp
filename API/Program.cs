using API.Data;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen();

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
});

// Add Google as an external login provider (only if credentials are configured).
if (!string.IsNullOrEmpty(builder.Configuration["Google:ClientId"]))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
    });
}


// =============================================
// SECTION 4: OTHER SERVICES
// =============================================
// Register our custom TokenService so we can inject it into controllers
builder.Services.AddScoped<TokenService>();

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
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapGet("/api/", () => "Hello World");

app.Run();
