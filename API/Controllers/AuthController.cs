using API.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.DTO;
using Shared.Models;


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
        // GOOGLE LOGIN — POST /api/auth/google
        // ==========================================
        // Receives a Google ID token from the mobile app, validates it,
        // and creates or finds the user in our database.
        //
        // The flow from the mobile app's perspective:
        // 1. User taps "Sign in with Google" on the phone
        // 2. Google's sign-in UI appears (handled by Google's SDK or WebAuthenticator)
        // 3. User picks their Google account and approves
        // 4. Google gives the mobile app an ID token (a JWT signed by Google)
        // 5. Mobile app sends that token to THIS endpoint
        // 6. We verify it's real, find/create the user, and return OUR JWT
        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] ExternalAuthDto dto)
        {
            // STEP 1: Validate the Google ID token
            // This is crucial — we're asking Google's library to verify that:
            // - The token was actually issued by Google (not forged)
            // - The token was intended for our app (audience check)
            // - The token hasn't expired
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    // This ensures the token was meant for YOUR app specifically.
                    // Without this check, someone could take a Google token issued
                    // for a different app and use it to log into yours.
                    Audience = new[] { _config["Google:ClientId"] }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);
            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new { message = "Invalid Google token" });
            }

            // STEP 2: The token is valid. Now find or create the user.
            // payload.Email, payload.GivenName, payload.FamilyName, payload.Picture
            // are all provided by Google from the user's Google account.
            var user = await _userManager.FindByEmailAsync(payload.Email);

            if (user == null)
            {
                // First time this Google user is logging in — create an account.
                // Notice: no password! This user authenticates through Google,
                // so we don't need (or want) a password in our database.
                user = new ApplicationUser
                {
                    UserName = payload.Email,
                    Email = payload.Email,
                    FirstName = payload.GivenName,
                    LastName = payload.FamilyName,
                    ProfileImageUrl = payload.Picture,
                    EmailConfirmed = true  // Google already verified their email
                };

                // CreateAsync WITHOUT a password parameter — important!
                // This creates a user that can only log in via external providers.
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                    return BadRequest(new { errors = createResult.Errors.Select(e => e.Description) });
            }

            // STEP 3: Link the Google login to the user
            // AspNetUserLogins table stores: UserId | LoginProvider | ProviderKey
            // This allows a single user to have multiple login methods
            // (e.g., email/password AND Google)
            var logins = await _userManager.GetLoginsAsync(user);
            if (!logins.Any(l => l.LoginProvider == "Google"))
            {
                // payload.Subject is Google's unique ID for this user
                var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
                await _userManager.AddLoginAsync(user, loginInfo);
            }

            // STEP 4: Generate OUR JWT token — from here on, it's identical
            // to a normal email/password login
            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                Email = user.Email!,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? ""
            });
        }
    }
}
