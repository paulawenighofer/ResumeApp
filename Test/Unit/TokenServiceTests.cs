using API.Services;
using Microsoft.Extensions.Configuration;
using Shared.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Test.Unit;

public class TokenServiceTests
{
    private static TokenService CreateService(int expiryMinutes = 60)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]                 = "TestSecretKeyThatIsLongEnoughForHS256!!",
                ["Jwt:Issuer"]              = "TestIssuer",
                ["Jwt:Audience"]            = "TestAudience",
                ["Jwt:ExpirationInMinutes"] = expiryMinutes.ToString(),
            })
            .Build();

        return new TokenService(config);
    }

    private static ApplicationUser MakeUser() => new()
    {
        Id             = Guid.NewGuid().ToString(),
        UserName       = "test@example.com",
        Email          = "test@example.com",
        FirstName      = "Jane",
        LastName       = "Doe",
        SecurityStamp  = Guid.NewGuid().ToString(),
    };

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var token = CreateService().GenerateToken(MakeUser());
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateToken_IsWellFormedJwt()
    {
        var token = CreateService().GenerateToken(MakeUser());
        // A valid JWT has exactly three base64url segments separated by dots
        Assert.Equal(3, token.Split('.').Length);
        Assert.True(new JwtSecurityTokenHandler().CanReadToken(token));
    }

    [Fact]
    public void GenerateToken_ContainsExpectedClaims()
    {
        var user  = MakeUser();
        var token = CreateService().GenerateToken(user);

        var jwt    = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var claims = jwt.Claims.ToList();

        Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
        Assert.Contains(claims, c => c.Type == ClaimTypes.Email          && c.Value == user.Email);
        Assert.Contains(claims, c => c.Type == "firstName"               && c.Value == user.FirstName);
        Assert.Contains(claims, c => c.Type == "lastName"                && c.Value == user.LastName);
        Assert.Contains(claims, c => c.Type == "security_stamp"          && c.Value == user.SecurityStamp);
    }

    [Fact]
    public void GenerateToken_ExpiresAtConfiguredTime()
    {
        var service = CreateService(expiryMinutes: 30);
        var before  = DateTime.UtcNow;
        var token   = service.GenerateToken(MakeUser());
        var after   = DateTime.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.True(jwt.ValidTo >= before.AddMinutes(29));
        Assert.True(jwt.ValidTo <= after.AddMinutes(31));
    }

    [Fact]
    public void GenerateToken_DifferentUsersProduceDifferentTokens()
    {
        var svc    = CreateService();
        var token1 = svc.GenerateToken(MakeUser());
        var token2 = svc.GenerateToken(MakeUser());
        Assert.NotEqual(token1, token2);
    }
}
