using System.Net.Http.Json;
using System.Text.Json;
using Test.Integration.Fixtures;

namespace Test.Integration;

/// <summary>
/// Static helpers shared across all integration test classes.
/// </summary>
internal static class AuthTestHelpers
{
    internal static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Registers a user and immediately verifies their email via OTP,
    /// returning the JWT from the verify-otp response.
    /// </summary>
    internal static async Task<string> RegisterAndVerifyAsync(
        HttpClient client,
        ApiFactory factory,
        string email = "user@example.com",
        string password = "Password1",
        string firstName = "Test",
        string lastName = "User")
    {
        // 1. Register
        var regRes = await client.PostAsJsonAsync("api/auth/register", new
        {
            firstName,
            lastName,
            email,
            password,
        });
        regRes.EnsureSuccessStatusCode();

        // 2. Grab the OTP the fake email service captured
        var code = factory.EmailService.LastOtpCode
            ?? throw new InvalidOperationException("FakeEmailService did not capture an OTP.");

        // 3. Verify OTP → get JWT
        var verifyRes = await client.PostAsJsonAsync("api/auth/verify-otp", new { email, code });
        verifyRes.EnsureSuccessStatusCode();

        var body = await verifyRes.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("No token in verify-otp response.");
    }

    /// <summary>
    /// Returns a new HttpClient with the Authorization header pre-set to the given JWT.
    /// </summary>
    internal static HttpClient CreateAuthenticatedClient(ApiFactory factory, string jwt)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
        return client;
    }
}
