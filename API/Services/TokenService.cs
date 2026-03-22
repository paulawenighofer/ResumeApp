using Microsoft.IdentityModel.Tokens;
using Shared.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API.Services
{
    public class TokenService
    {
        private readonly IConfiguration _config;

        // IConfiguration is injected by ASP.NET Core's dependency injection.
        // It gives us access to appsettings.json values.
        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(ApplicationUser user)
        {
            // CLAIMS: These are pieces of information embedded in the token.
            // When a protected endpoint receives this token, it can read these
            // claims to know who the user is without hitting the database.
            //
            // Think of claims like the info printed on your hotel keycard:
            // Room 302, Guest Name: Bishwas, Checkout: March 15
            var claims = new List<Claim>
        {
            // NameIdentifier is the standard claim for "user ID"
            // Your controllers will read this to know which user is making the request
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim("firstName", user.FirstName ?? ""),
            new Claim("lastName", user.LastName ?? "")
        };

            // Create the signing key from our secret.
            // SymmetricSecurityKey means the same key is used to SIGN and VERIFY.
            // (As opposed to asymmetric, where you have a public/private key pair.)
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            // SigningCredentials combines the key with the algorithm.
            // HmacSha256 is the industry standard for JWT signing — fast and secure.
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Build the token with all the pieces
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],       // Who created this token
                audience: _config["Jwt:Audience"],   // Who should accept this token
                claims: claims,                       // The user data inside the token
                expires: DateTime.UtcNow.AddMinutes(  // When it expires
                    double.Parse(_config["Jwt:ExpirationInMinutes"]!)),
                signingCredentials: creds             // The signature
            );

            // Serialize the token to a string like "eyJhbGciOiJIUzI1NiIs..."
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
