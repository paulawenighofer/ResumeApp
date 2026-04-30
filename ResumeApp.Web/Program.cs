using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using MudBlazor.Services;
using ResumeApp.Web.Components;
using ResumeApp.Web.Services;
using Shared.DTO;
using System.Net.Http.Headers;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/account/signout";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// Persist Data Protection keys to a mounted volume so they survive container restarts.
// Without this, each restart generates new keys and invalidates all existing
// antiforgery tokens and auth cookies, causing "key not found in key ring" errors.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/dataprotection-keys"))
    .SetApplicationName("ResumeApp.Web");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://resumeapp-web-api.victoriousriver-0bd90a87.westus2.azurecontainerapps.io/";

builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(90);
});

// Named client used by proxy endpoints (not DI-scoped to ApiClient)
builder.Services.AddHttpClient("proxy", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(90);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ── Sign-in endpoint ──────────────────────────────────────────────────────
// Interactive components can't call HttpContext.SignInAsync() directly (no HTTP context
// in the SignalR circuit). Instead they store auth data in the short-lived cache and
// redirect here with a one-time key so the server can issue the cookie.
app.MapGet("/account/signin", async (
    string key,
    IMemoryCache cache,
    HttpContext ctx) =>
{
    if (!cache.TryGetValue(key, out AuthSignInData? data) || data is null)
        return Results.Redirect("/login?error=session_expired");

    cache.Remove(key);

    var claims = new List<Claim>
    {
        new(ClaimTypes.Email, data.Email),
        new(ClaimTypes.GivenName, data.FirstName),
        new(ClaimTypes.Surname, data.LastName),
        new("jwt", data.Token),
        new("profileImageUrl", data.ProfileImageUrl ?? string.Empty)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Redirect("/dashboard");
});

// ── Social OAuth callback ─────────────────────────────────────────────────
// The API redirects here after completing OAuth with Google/GitHub/LinkedIn.
// The JWT arrives as ?token= query param. We fetch /api/auth/me to get the
// user's name, then issue the auth cookie.
app.MapGet("/auth/social-callback", async (
    string? token,
    string? error,
    HttpContext ctx,
    IHttpClientFactory factory,
    IConfiguration config) =>
{
    if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(token))
        return Results.Redirect("/login?error=social_auth_failed");

    var apiBase = config["ApiBaseUrl"] ?? "https://resumeapp-web-api.victoriousriver-0bd90a87.westus2.azurecontainerapps.io/";
    var http = factory.CreateClient("proxy");
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    AuthResponseDto? me;
    try
    {
        me = await http.GetFromJsonAsync<AuthResponseDto>($"{apiBase}/api/auth/me");
    }
    catch
    {
        return Results.Redirect("/login?error=social_auth_failed");
    }

    if (me is null)
        return Results.Redirect("/login?error=social_auth_failed");

    var claims = new List<Claim>
    {
        new(ClaimTypes.Email, me.Email),
        new(ClaimTypes.GivenName, me.FirstName),
        new(ClaimTypes.Surname, me.LastName),
        new("jwt", token),
        new("profileImageUrl", me.ProfileImageUrl ?? string.Empty)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Redirect("/dashboard");
});

// ── Sign-out endpoint ─────────────────────────────────────────────────────
app.MapGet("/account/signout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

// ── PDF proxy ─────────────────────────────────────────────────────────────
// Streams the PDF from the API to the browser with authentication.
// The Blazor component navigates here with forceLoad=true so the browser
// triggers a file download.
app.MapGet("/proxy/resumes/{id}/pdf", async (
    int id,
    HttpContext ctx,
    IHttpClientFactory factory,
    IConfiguration config) =>
{
    var jwt = ctx.User.FindFirstValue("jwt");
    if (string.IsNullOrEmpty(jwt))
        return Results.Unauthorized();

    var apiBase = config["ApiBaseUrl"] ?? "https://resumeapp-web-api.victoriousriver-0bd90a87.westus2.azurecontainerapps.io/";
    var http = factory.CreateClient("proxy");
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

    HttpResponseMessage response;
    try
    {
        response = await http.GetAsync($"{apiBase}/api/resumes/{id}/pdf");
    }
    catch
    {
        return Results.Problem("Could not reach API.");
    }

    if (!response.IsSuccessStatusCode)
        return Results.NotFound();

    var stream = await response.Content.ReadAsStreamAsync();
    return Results.Stream(stream, "application/pdf", $"resume-{id}.pdf");
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
