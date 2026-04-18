using System.Text;
using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class SocialAuthController : ControllerBase
{
    private readonly SocialAuthService _socialAuth;
    private readonly IConfiguration _config;
    private readonly ApiMetrics _metrics;
    private readonly ILogger<SocialAuthController> _logger;

    public SocialAuthController(SocialAuthService socialAuth, IConfiguration config, ApiMetrics metrics, ILogger<SocialAuthController> logger)
    {
        _socialAuth = socialAuth;
        _config = config;
        _metrics = metrics;
        _logger = logger;
    }

    // ============================================================
    // GOOGLE
    // ============================================================

    /// <summary>
    /// GET /api/auth/google-challenge
    ///
    /// The mobile app opens a browser to this URL. This endpoint doesn't
    /// return JSON — it REDIRECTS the browser to Google's login page.
    ///
    /// The "state" parameter is a security measure called a CSRF token.
    /// We generate a random string and send it to Google. When Google
    /// calls us back, it includes this same state. If it doesn't match,
    /// someone is trying to hijack the flow.
    ///
    /// Accepts an optional returnUrl for Windows desktop support — the
    /// local HTTP listener's address is passed here so the callback knows
    /// where to redirect after OAuth completes.
    /// </summary>
    [HttpGet("google-challenge")]
    public IActionResult GoogleChallenge([FromQuery] string? returnUrl = null)
    {
        var redirectUri = GetCallbackUri("google");
        var state = BuildState(returnUrl);
        var authUrl = _socialAuth.BuildGoogleAuthUrl(redirectUri, state);
        return Redirect(authUrl);
    }

    /// <summary>
    /// GET /api/auth/google-callback?code=xxx&state=yyy
    ///
    /// Google redirects here after the user signs in successfully.
    /// We exchange the code for a token, fetch the profile, and redirect
    /// back to our MAUI app with our JWT token.
    ///
    /// On Windows desktop, the state contains a base64-encoded returnUrl
    /// (the local HTTP listener). On mobile, it redirects to the app scheme.
    /// </summary>
    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string code, [FromQuery] string state)
    {
        return await HandleCallbackAsync(
            TelemetryTags.Providers.Google,
            "google",
            state,
            "google_auth_failed",
            redirectUri => _socialAuth.HandleGoogleCallbackAsync(code, redirectUri));
    }

    // ============================================================
    // LINKEDIN
    // ============================================================

    [HttpGet("linkedin-challenge")]
    public IActionResult LinkedInChallenge([FromQuery] string? returnUrl = null)
    {
        var redirectUri = GetCallbackUri("linkedin");
        var state = BuildState(returnUrl);
        var authUrl = _socialAuth.BuildLinkedInAuthUrl(redirectUri, state);
        return Redirect(authUrl);
    }

    [HttpGet("linkedin-callback")]
    public async Task<IActionResult> LinkedInCallback(
        [FromQuery] string code, [FromQuery] string state)
    {
        return await HandleCallbackAsync(
            TelemetryTags.Providers.LinkedIn,
            "linkedin",
            state,
            "linkedin_auth_failed",
            redirectUri => _socialAuth.HandleLinkedInCallbackAsync(code, redirectUri));
    }

    // ============================================================
    // GITHUB
    // ============================================================

    [HttpGet("github-challenge")]
    public IActionResult GitHubChallenge([FromQuery] string? returnUrl = null)
    {
        var redirectUri = GetCallbackUri("github");
        var state = BuildState(returnUrl);
        var authUrl = _socialAuth.BuildGitHubAuthUrl(redirectUri, state);
        return Redirect(authUrl);
    }

    [HttpGet("github-callback")]
    public async Task<IActionResult> GitHubCallback(
        [FromQuery] string code, [FromQuery] string state)
    {
        return await HandleCallbackAsync(
            TelemetryTags.Providers.GitHub,
            "github",
            state,
            "github_auth_failed",
            redirectUri => _socialAuth.HandleGitHubCallbackAsync(code, redirectUri));
    }

    // ============================================================
    // SHARED HELPERS
    // ============================================================

    /// <summary>
    /// Builds the callback URI using the configured ApiBaseUrl instead of
    /// Request.Host. This is critical because:
    /// - Android emulator sends requests via 10.0.2.2 (private IP)
    /// - Google/LinkedIn/GitHub reject private IPs as redirect URIs
    /// - The registered redirect URI in provider consoles uses localhost
    ///
    /// By always using ApiBaseUrl (which is "https://localhost:7082" in dev),
    /// the redirect URI matches what's registered in the provider consoles
    /// regardless of where the request originated from.
    /// </summary>
    private string GetCallbackUri(string provider)
    {
        var baseUrl = _config["ApiBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        return $"{baseUrl}/api/auth/{provider}-callback";
    }

    /// <summary>
    /// Packs the CSRF token and optional desktop returnUrl into the state
    /// parameter. The state survives the entire OAuth round-trip — the
    /// provider sends it back unchanged in the callback.
    ///
    /// Format: "csrfToken" (mobile) or "csrfToken|base64(returnUrl)" (desktop)
    ///
    /// Why Base64? The returnUrl contains special characters (://) that could
    /// break URL parsing. Base64 makes it safe to embed in a query string.
    /// </summary>
    private static string BuildState(string? returnUrl)
    {
        var csrfToken = Guid.NewGuid().ToString("N");
        if (string.IsNullOrEmpty(returnUrl))
            return csrfToken;

        var encodedUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(returnUrl));
        return $"{csrfToken}|{encodedUrl}";
    }

    /// <summary>
    /// After the OAuth flow completes, this method decides where to
    /// redirect the browser:
    ///
    /// - If returnUrl exists in state (Windows desktop):
    ///   redirect to http://localhost:{port}?token=xxx
    ///   The MAUI app's local HTTP listener catches this.
    ///
    /// - If no returnUrl (mobile):
    ///   redirect to myresumebuilder://auth?token=xxx
    ///   The OS opens the MAUI app via WebAuthenticator.
    /// </summary>
    private RedirectResult BuildCallbackRedirect(
        string? token, string state, string errorCode)
    {
        string? returnUrl = null;
        if (state.Contains('|'))
        {
            try
            {
                var parts = state.Split('|', 2);
                returnUrl = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
            }
            catch
            {
                returnUrl = null;
            }
        }

        var appScheme = _config["AppScheme"] ?? "myresumebuilder";

        if (token == null)
        {
            var errorUrl = !string.IsNullOrEmpty(returnUrl)
                ? $"{returnUrl}?error={errorCode}"
                : $"{appScheme}://auth?error={errorCode}";
            return Redirect(errorUrl);
        }

        var successUrl = !string.IsNullOrEmpty(returnUrl)
            ? $"{returnUrl}?token={token}"
            : $"{appScheme}://auth?token={token}";
        return Redirect(successUrl);
    }

    private async Task<IActionResult> HandleCallbackAsync(
        string provider,
        string providerRoute,
        string state,
        string errorCode,
        Func<string, Task<string?>> callbackHandler)
    {
        var redirectUri = GetCallbackUri(providerRoute);

        try
        {
            var token = await callbackHandler(redirectUri);

            if (token is null)
            {
                _logger.LogError(
                    "Social login callback returned no application token for provider {Provider}",
                    provider);
                _metrics.RecordSocialLogin(provider, TelemetryTags.Outcomes.Failure);
                return BuildCallbackRedirect(null, state, errorCode);
            }

            _metrics.RecordSocialLogin(provider, TelemetryTags.Outcomes.Success);
            return BuildCallbackRedirect(token, state, errorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Social login callback processing failed for provider {Provider}",
                provider);
            _metrics.RecordSocialLogin(provider, TelemetryTags.Outcomes.Failure);
            return BuildCallbackRedirect(null, state, errorCode);
        }
    }
}
