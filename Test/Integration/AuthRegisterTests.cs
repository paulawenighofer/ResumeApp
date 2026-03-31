using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using API.Data;
using Test.Integration.Fixtures;

namespace Test.Integration;

public class AuthRegisterTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthRegisterTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.EmailService.Reset();
    }

    [Fact]
    public async Task Register_ValidData_Returns200_AndSendsOtp()
    {
        var res = await _client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "Alice",
            lastName = "Smith",
            email = "alice@example.com",
            password = "Password1",
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.Equal("alice@example.com", body.GetProperty("email").GetString());

        // OTP was sent via fake email service
        Assert.Equal("alice@example.com", _factory.EmailService.LastOtpEmail);
        Assert.NotNull(_factory.EmailService.LastOtpCode);
        Assert.Equal(6, _factory.EmailService.LastOtpCode!.Length);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var payload = new
        {
            firstName = "Bob",
            lastName = "Jones",
            email = "bob@example.com",
            password = "Password1",
        };

        await _client.PostAsJsonAsync("api/auth/register", payload);
        var res = await _client.PostAsJsonAsync("api/auth/register", payload);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var res = await _client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "Carl",
            lastName = "Lee",
            email = "carl@example.com",
            password = "weak",           // too short, no uppercase, no digit
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmailFormat_Returns400()
    {
        var res = await _client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "Dana",
            lastName = "White",
            email = "not-an-email",
            password = "Password1",
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_MissingFirstName_Returns400()
    {
        var res = await _client.PostAsJsonAsync("api/auth/register", new
        {
            lastName = "White",
            email = "dana2@example.com",
            password = "Password1",
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_WhenEmailFails_Returns500_AndUserIsNotPersisted()
    {
        _factory.EmailService.ShouldThrow = true;

        var res = await _client.PostAsJsonAsync("api/auth/register", new
        {
            firstName = "Eve",
            lastName = "Fail",
            email = "eve@example.com",
            password = "Password1",
        });

        Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);

        // Verify the user was rolled back
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.FirstOrDefault(u => u.Email == "eve@example.com");
        Assert.Null(user);
    }
}
