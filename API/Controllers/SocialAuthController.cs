using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class SocialAuthController : ControllerBase
{
    private readonly SocialAuthService _socialAuth;
    private readonly IConfiguration _config;

    public SocialAuthController(SocialAuthService socialAuth, IConfiguration config)
    {
        _socialAuth = socialAuth;
        _config = config;
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
    /// </summary>
    [HttpGet("google-challenge")]
    public IActionResult GoogleChallenge()
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/google-callback";
        var state = Guid.NewGuid().ToString("N");
        var authUrl = _socialAuth.BuildGoogleAuthUrl(redirectUri, state);
        return Redirect(authUrl);
    }

    /// <summary>
    /// GET /api/auth/google-callback?code=xxx&state=yyy
    /// 
    /// Google redirects here after the user signs in successfully.
    /// We exchange the code for a token, fetch the profile, and redirect
    /// back to our MAUI app with our JWT token.
    /// </summary>
    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string code, [FromQuery] string state)
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/google-callback";
        var token = await _socialAuth.HandleGoogleCallbackAsync(code, redirectUri);

        if (token == null)
            return Redirect($"{_config["AppScheme"]}://auth?error=google_auth_failed");

        return Redirect($"{_config["AppScheme"]}://auth?token={token}");
    }

    // ============================================================
    // LINKEDIN
    // ============================================================

    [HttpGet("linkedin-challenge")]
    public IActionResult LinkedInChallenge()
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/linkedin-callback";
        var state = Guid.NewGuid().ToString("N");
        var authUrl = _socialAuth.BuildLinkedInAuthUrl(redirectUri, state);
        return Redirect(authUrl);
    }

    [HttpGet("linkedin-callback")]
    public async Task<IActionResult> LinkedInCallback(
        [FromQuery] string code, [FromQuery] string state)
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/linkedin-callback";
        var token = await _socialAuth.HandleLinkedInCallbackAsync(code, redirectUri);

        if (token == null)
            return Redirect($"{_config["AppScheme"]}://auth?error=linkedin_auth_failed");

        return Redirect($"{_config["AppScheme"]}://auth?token={token}");
    }

    // ============================================================
    // GITHUB
    // ============================================================

    [HttpGet("github-challenge")]
    public IActionResult GitHubChallenge()
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/github-callback";
        var state = Guid.NewGuid().ToString("N");
        var authUrl = _socialAuth.BuildGitHubAuthUrl(redirectUri, state);
        return Redirect(authUrl);
    }

    [HttpGet("github-callback")]
    public async Task<IActionResult> GitHubCallback(
        [FromQuery] string code, [FromQuery] string state)
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/github-callback";
        var token = await _socialAuth.HandleGitHubCallbackAsync(code, redirectUri);

        if (token == null)
            return Redirect($"{_config["AppScheme"]}://auth?error=github_auth_failed");

        return Redirect($"{_config["AppScheme"]}://auth?token={token}");
    }
}