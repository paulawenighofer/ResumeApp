using System.Net.Http.Headers;
using System.Text.Json;
using Shared.Models;
using Microsoft.AspNetCore.Identity;

namespace API.Services;

/// <summary>
/// Handles the OAuth 2.0 Authorization Code flow for all social providers.
/// The flow has three steps for every provider:
/// 1. Build an authorization URL → user visits it in browser → signs in with provider
/// 2. Provider redirects back with a code → we exchange code for access token
/// 3. Use access token to fetch user profile → create/find user → return JWT
/// </summary>
public class SocialAuthService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;

    public SocialAuthService(
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        UserManager<ApplicationUser> userManager,
        TokenService tokenService)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _userManager = userManager;
        _tokenService = tokenService;
    }

    // ============================================================
    // GOOGLE
    // ============================================================

    /// <summary>
    /// Builds the URL that the user's browser should be redirected to
    /// for Google sign-in.
    /// 
    /// The "state" parameter is a random string that prevents CSRF attacks.
    /// We send it to Google, and Google sends it back in the callback.
    /// If it doesn't match, someone is trying to trick our callback endpoint.
    /// 
    /// The "scope" parameter tells Google what data we want access to.
    /// "openid profile email" means: verify their identity and give us
    /// their name and email address.
    /// </summary>
    public string BuildGoogleAuthUrl(string redirectUri, string state)
    {
        var clientId = _config["Google:ClientId"];
        return $"https://accounts.google.com/o/oauth2/v2/auth" +
               $"?response_type=code" +
               $"&client_id={clientId}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&scope=openid%20email%20profile";
    }

    /// <summary>
    /// After the user signs in with Google, Google redirects back to our
    /// callback URL with a "code" parameter. This method:
    /// 1. Exchanges that code for an access token
    /// 2. Uses the access token to fetch the user's profile
    /// 3. Creates or finds the user in our database
    /// 4. Returns our JWT token
    /// </summary>
    public async Task<string?> HandleGoogleCallbackAsync(string code, string redirectUri)
    {
        var client = _httpClientFactory.CreateClient();

        // STEP 1: Exchange the authorization code for an access token
        var tokenResponse = await client.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = _config["Google:ClientId"]!,
                ["client_secret"] = _config["Google:ClientSecret"]!
            }));

        if (!tokenResponse.IsSuccessStatusCode)
            return null;

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenJson.GetProperty("access_token").GetString();

        // STEP 2: Use the access token to fetch the user's profile
        var profileRequest = new HttpRequestMessage(HttpMethod.Get,
            "https://www.googleapis.com/oauth2/v2/userinfo");
        profileRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var profileResponse = await client.SendAsync(profileRequest);
        if (!profileResponse.IsSuccessStatusCode)
            return null;

        var profile = await profileResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Google profile: { "id": "123", "email": "user@gmail.com",
        //   "given_name": "Bishwas", "family_name": "Thapa",
        //   "picture": "https://..." }
        var email = profile.GetProperty("email").GetString()!;
        var firstName = profile.TryGetProperty("given_name", out var fn) ? fn.GetString() : "";
        var lastName = profile.TryGetProperty("family_name", out var ln) ? ln.GetString() : "";
        var picture = profile.TryGetProperty("picture", out var pic) ? pic.GetString() : null;
        var googleId = profile.GetProperty("id").GetString()!;

        return await FindOrCreateUserAndGenerateTokenAsync(
            email, firstName, lastName, picture, "Google", googleId);
    }

    // ============================================================
    // LINKEDIN
    // ============================================================

    /// <summary>
    /// Builds the LinkedIn authorization URL. Same pattern as Google —
    /// only the URLs and scope format differ.
    /// 
    /// LinkedIn's scope "openid profile email" uses the OpenID Connect
    /// standard, which gives us the user's name, email, and profile picture.
    /// </summary>
    public string BuildLinkedInAuthUrl(string redirectUri, string state)
    {
        var clientId = _config["LinkedIn:ClientId"];
        return $"https://www.linkedin.com/oauth/v2/authorization" +
               $"?response_type=code" +
               $"&client_id={clientId}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&scope=openid%20profile%20email";
    }

    /// <summary>
    /// Same three-step flow as Google:
    /// 1. Exchange code for access token
    /// 2. Fetch user profile
    /// 3. Create/find user and return JWT
    /// 
    /// The only differences from Google are the endpoint URLs and
    /// the shape of the JSON response.
    /// </summary>
    public async Task<string?> HandleLinkedInCallbackAsync(string code, string redirectUri)
    {
        var client = _httpClientFactory.CreateClient();

        // STEP 1: Exchange code for access token
        var tokenResponse = await client.PostAsync(
            "https://www.linkedin.com/oauth/v2/accessToken",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = _config["LinkedIn:ClientId"]!,
                ["client_secret"] = _config["LinkedIn:ClientSecret"]!
            }));

        if (!tokenResponse.IsSuccessStatusCode)
            return null;

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenJson.GetProperty("access_token").GetString();

        // STEP 2: Fetch user profile
        // LinkedIn's userinfo endpoint follows the OpenID Connect standard
        var profileRequest = new HttpRequestMessage(HttpMethod.Get,
            "https://api.linkedin.com/v2/userinfo");
        profileRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var profileResponse = await client.SendAsync(profileRequest);
        if (!profileResponse.IsSuccessStatusCode)
            return null;

        var profile = await profileResponse.Content.ReadFromJsonAsync<JsonElement>();

        // LinkedIn profile: { "sub": "abc123", "email": "user@email.com",
        //   "given_name": "Bishwas", "family_name": "Thapa",
        //   "picture": "https://..." }
        var email = profile.GetProperty("email").GetString()!;
        var firstName = profile.TryGetProperty("given_name", out var fn) ? fn.GetString() : "";
        var lastName = profile.TryGetProperty("family_name", out var ln) ? ln.GetString() : "";
        var picture = profile.TryGetProperty("picture", out var pic) ? pic.GetString() : null;
        var linkedInId = profile.GetProperty("sub").GetString()!;

        return await FindOrCreateUserAndGenerateTokenAsync(
            email, firstName, lastName, picture, "LinkedIn", linkedInId);
    }

    // ============================================================
    // GITHUB
    // ============================================================

    /// <summary>
    /// Builds the GitHub authorization URL. Same pattern as the others.
    /// 
    /// GitHub's scope "user:email" gives us access to the user's email.
    /// "read:user" gives us access to their profile info (name, avatar).
    /// </summary>
    public string BuildGitHubAuthUrl(string redirectUri, string state)
    {
        var clientId = _config["GitHub:ClientId"];
        return $"https://github.com/login/oauth/authorize" +
               $"?client_id={clientId}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&scope=user:email%20read:user";
    }

    /// <summary>
    /// Same three-step flow, with one GitHub-specific quirk:
    /// Their /user endpoint might not include the email if the user
    /// has set it to private. In that case, we make a separate call
    /// to /user/emails to get it.
    /// </summary>
    public async Task<string?> HandleGitHubCallbackAsync(string code, string redirectUri)
    {
        var client = _httpClientFactory.CreateClient();

        // STEP 1: Exchange code for access token
        // GitHub quirk: it only returns JSON if you set the Accept header
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post,
            "https://github.com/login/oauth/access_token");
        tokenRequest.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        tokenRequest.Content = JsonContent.Create(new
        {
            client_id = _config["GitHub:ClientId"],
            client_secret = _config["GitHub:ClientSecret"],
            code = code,
            redirect_uri = redirectUri
        });

        var tokenResponse = await client.SendAsync(tokenRequest);
        if (!tokenResponse.IsSuccessStatusCode)
            return null;

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenJson.GetProperty("access_token").GetString();

        // STEP 2: Fetch user profile
        // GitHub quirk: requires a User-Agent header on all API requests
        var profileRequest = new HttpRequestMessage(HttpMethod.Get,
            "https://api.github.com/user");
        profileRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        profileRequest.Headers.UserAgent.ParseAdd("ResumeBuilderApp");

        var profileResponse = await client.SendAsync(profileRequest);
        if (!profileResponse.IsSuccessStatusCode)
            return null;

        var profile = await profileResponse.Content.ReadFromJsonAsync<JsonElement>();

        // GitHub profile: { "id": 12345, "login": "bishwascodes",
        //   "name": "Bishwas Thapa", "email": "user@email.com",
        //   "avatar_url": "https://..." }
        var gitHubId = profile.GetProperty("id").GetInt64().ToString();
        var name = profile.TryGetProperty("name", out var n) && n.ValueKind != JsonValueKind.Null
            ? n.GetString() : "";
        var email = profile.TryGetProperty("email", out var e) && e.ValueKind != JsonValueKind.Null
            ? e.GetString() : null;
        var avatar = profile.TryGetProperty("avatar_url", out var av)
            ? av.GetString() : null;

        // STEP 2B: If email is null (user has private email), fetch it separately
        // This is a GitHub-specific quirk — Google and LinkedIn always include email
        if (string.IsNullOrEmpty(email))
        {
            var emailRequest = new HttpRequestMessage(HttpMethod.Get,
                "https://api.github.com/user/emails");
            emailRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
            emailRequest.Headers.UserAgent.ParseAdd("ResumeBuilderApp");

            var emailResponse = await client.SendAsync(emailRequest);
            if (emailResponse.IsSuccessStatusCode)
            {
                var emails = await emailResponse.Content
                    .ReadFromJsonAsync<JsonElement>();

                // GitHub returns an array of emails. We want the primary one.
                // [{ "email": "user@email.com", "primary": true, "verified": true }, ...]
                foreach (var emailEntry in emails.EnumerateArray())
                {
                    if (emailEntry.TryGetProperty("primary", out var primary)
                        && primary.GetBoolean())
                    {
                        email = emailEntry.GetProperty("email").GetString();
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(email))
            return null; // Can't create a user without an email

        // GitHub gives a single "name" field, not first/last separately
        var nameParts = (name ?? "").Split(' ', 2);
        var firstName = nameParts.Length > 0 ? nameParts[0] : "";
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";

        return await FindOrCreateUserAndGenerateTokenAsync(
            email, firstName, lastName, avatar, "GitHub", gitHubId);
    }

    // ============================================================
    // SHARED: Find or create user, link external login, generate JWT
    // ============================================================

    /// <summary>
    /// This method is used by ALL social login providers. It handles the
    /// common logic of:
    /// 1. Check if a user with this email already exists
    /// 2. If not, create a new user (no password — social-only account)
    /// 3. Link the social login to the user (so they can log in with it again)
    /// 4. Generate and return our JWT token
    /// 
    /// The "provider" and "providerKey" are stored in the AspNetUserLogins table.
    /// This allows a single user to have multiple login methods — for example,
    /// they could sign up with email/password AND link their Google later.
    /// </summary>
    private async Task<string?> FindOrCreateUserAndGenerateTokenAsync(
        string email, string? firstName, string? lastName,
        string? profileImageUrl, string provider, string providerKey)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                ProfileImageUrl = profileImageUrl,
                EmailConfirmed = true // Provider already verified the email
            };

            // CreateAsync WITHOUT a password — this user authenticates
            // through social login only, no password in our database
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
                return null;
        }
        else if (!user.EmailConfirmed)
        {
            // User existed before social login was added, or signed up with
            // email/password and never confirmed — the provider has now verified
            // this email, so mark it confirmed
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        // Link the social login if not already linked
        // This is stored in the AspNetUserLogins table:
        // UserId | LoginProvider | ProviderKey
        // This means one user can have multiple login methods
        var logins = await _userManager.GetLoginsAsync(user);
        if (!logins.Any(l => l.LoginProvider == provider))
        {
            await _userManager.AddLoginAsync(user,
                new UserLoginInfo(provider, providerKey, provider));
        }

        return _tokenService.GenerateToken(user);
    }
}
