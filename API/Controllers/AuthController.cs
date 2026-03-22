using API.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.DTO;
using Shared.Models;
using System.Security.Claims;


namespace API.Controllers
{

    [ApiController]                    // Enables automatic model validation and routing
    [Route("api/[controller]")]        // URL will be: /api/auth (derived from "AuthController")
    public class AuthController : ControllerBase
    {
        // These are all injected by ASP.NET Core's dependency injection.
        // We registered them in Program.cs, and the framework automatically
        // provides them when creating the controller.

        private readonly UserManager<ApplicationUser> _userManager;
        // UserManager handles user operations: create, find, update, delete, check password.
        // It's the main class you interact with from Identity.

        private readonly SignInManager<ApplicationUser> _signInManager;
        // SignInManager handles sign-in logic: password verification, lockout tracking,
        // external login management. It works together with UserManager.

        private readonly TokenService _tokenService;
        // Our custom service that generates JWT tokens.

        private readonly IConfiguration _config;
        // Access to appsettings.json values (we need the Google ClientId).

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TokenService tokenService,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _config = config;
        }


        // ==========================================
        // REGISTER — POST /api/auth/register
        // ==========================================
        // Creates a new user account with email and password.
        //
        // What happens step by step:
        // 1. ASP.NET validates the RegisterDto (checks [Required], [EmailAddress], etc.)
        // 2. We create an ApplicationUser object
        // 3. UserManager.CreateAsync hashes the password and saves to the database
        // 4. If successful, we generate a JWT and send it back
        // 5. The mobile app stores the JWT and uses it for future requests
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // If model validation failed (missing required fields, bad email format),
            // ASP.NET already caught it. But we check just in case.
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Create the user object. Note: we use email as the username.
            // This simplifies things — users only need to remember their email.
            var user = new ApplicationUser
            {
                UserName = dto.Email,      // Identity requires a UserName
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName
            };

            // CreateAsync does ALL of this internally:
            // 1. Checks if email is already taken (because we set RequireUniqueEmail)
            // 2. Validates the password against our rules (8+ chars, uppercase, digit, etc.)
            // 3. Hashes the password using PBKDF2 with a random salt
            // 4. Inserts the user into the AspNetUsers table
            // 5. Returns a result indicating success or failure with specific errors
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                // Common errors: "Email already taken", "Password too weak"
                // Identity provides descriptive error messages we can forward to the client
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            // User created successfully — generate a token so they're immediately logged in
            // (no need to register and then login separately)
            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Email = user.Email!,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? ""
            });
        }


        // ==========================================
        // LOGIN — POST /api/auth/login
        // ==========================================
        // Verifies email + password and returns a JWT token.
        //
        // Why we don't tell the client whether the email or password was wrong:
        // Security best practice. If you say "email not found," an attacker knows
        // that email doesn't exist. If you say "wrong password," they know the email
        // DOES exist. By always saying "Invalid email or password," they learn nothing.
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Try to find the user by email
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password" });

            // CheckPasswordSignInAsync does ALL of this:
            // 1. Retrieves the stored password hash from the database
            // 2. Hashes the provided password with the same salt
            // 3. Compares the two hashes (never compares plain text passwords!)
            // 4. If lockoutOnFailure is true, tracks failed attempts
            // 5. If max attempts reached, locks the account for the configured duration
            var result = await _signInManager.CheckPasswordSignInAsync(
                user, dto.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
                return StatusCode(423, new { message = "Account locked. Try again later." });

            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid email or password" });

            // Password correct — generate token
            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Email = user.Email!,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? ""
            });
        }


        // ==========================================
        // ME — GET /api/auth/me
        // ==========================================
        // Returns the current user's profile from the database.
        // Used by the MAUI app to populate the main page after social login,
        // where only a token is returned (not full user info).
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null) return Unauthorized();

            return Ok(new AuthResponseDto
            {
                Token = "",   // Not returning a new token — caller already has one
                Email = user.Email!,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? ""
            });
        }


        // ==========================================
        // LOGOUT — POST /api/auth/logout
        // ==========================================
        // Logs out from the current device.
        // JWTs are stateless so the server doesn't store sessions — the actual
        // logout is handled by the client deleting its local token.
        // This endpoint exists as a clean contract and for any future
        // server-side cleanup (audit logs, token blocklists, etc.).
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logged out successfully." });
        }


        // ==========================================
        // LOGOUT ALL — POST /api/auth/logout-all
        // ==========================================
        // Logs out from ALL devices by rotating the SecurityStamp.
        // Every JWT we issue embeds the SecurityStamp as a claim.
        // Program.cs validates that claim against the database on each request.
        // Once the stamp changes, every token issued before this call is instantly
        // rejected — even if it hasn't expired yet.
        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null) return Unauthorized();

            await _userManager.UpdateSecurityStampAsync(user);
            return Ok(new { message = "Logged out from all devices." });
        }
    }
}
