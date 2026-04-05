using API.Data;
using API.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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

        private readonly IEmailService _emailService;
        // Sends OTP codes and password-reset links via SMTP.

        private readonly AppDbContext _db;
        // Direct DB access needed for OtpVerifications table queries.

        private readonly IConfiguration _config;
        // Access to appsettings.json values (we need the Google ClientId).

        private readonly ApiMetrics _metrics;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TokenService tokenService,
            IEmailService emailService,
            AppDbContext db,
            IConfiguration config,
            ApiMetrics metrics)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _db = db;
            _config = config;
            _metrics = metrics;
        }


        // ==========================================
        // REGISTER — POST /api/auth/register
        // ==========================================
        // Creates a new user account with email and password.
        //
        // What happens step by step:
        // 1. ASP.NET validates the RegisterDto (checks [Required], [EmailAddress], etc.)
        // 2. We create an ApplicationUser object (EmailConfirmed = false by default)
        // 3. UserManager.CreateAsync hashes the password and saves to the database
        // 4. A 6-digit OTP is generated, stored in OtpVerifications, and emailed
        // 5. We return the email so the client can navigate to the OTP page
        //    (no JWT yet — user must verify email before getting access)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // If model validation failed (missing required fields, bad email format),
            // ASP.NET already caught it. But we check just in case.
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Create the user object. Note: we use email as the username.
            // This simplifies things — users only need to remember their email.
            // EmailConfirmed stays false until they enter the OTP.
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
                _metrics.RecordRegistration(TelemetryTags.Outcomes.Failure);
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            try
            {
                // Send OTP — user must verify before they can log in
                await SendOtpToUserAsync(user, OtpPurpose.EmailVerification);
            }
            catch (Exception ex)
            {
                // Don't leave a half-registered account if OTP email could not be sent.
                await _userManager.DeleteAsync(user);
                _metrics.RecordRegistration(TelemetryTags.Outcomes.Failure);
                return StatusCode(500, new { message = $"Failed to send verification email: {ex.Message}" });
            }

            _metrics.RecordRegistration(TelemetryTags.Outcomes.Success);
            return Ok(new RegisterPendingResponseDto
            {
                Email = user.Email!,
                Message = "A 6-digit verification code has been sent to your email."
            });
        }


        // ==========================================
        // VERIFY OTP — POST /api/auth/verify-otp
        // ==========================================
        // Validates the 6-digit code the user received by email.
        // On success: marks EmailConfirmed = true, deletes the OTP record,
        // and returns a JWT so the user is immediately logged in.
        [HttpPost("verify-otp")]
        [EnableRateLimiting("otp-verify")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                _metrics.RecordOtpVerification(TelemetryTags.Outcomes.Failure);
                return BadRequest(new { message = "Invalid request." });
            }

            // Fetch the most recent email-verification OTP for this user
            var otp = await _db.OtpVerifications
                .Where(o => o.UserId == user.Id && o.Purpose == OtpPurpose.EmailVerification)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            // Verify the submitted code against the stored BCrypt hash
            if (otp == null || otp.ExpiresAt < DateTime.UtcNow || !OtpHasher.Verify(dto.Code, otp.Code))
            {
                _metrics.RecordOtpVerification(TelemetryTags.Outcomes.Failure);
                return BadRequest(new { message = "Invalid or expired code." });
            }

            // Mark email as confirmed and clean up the OTP record
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            _db.OtpVerifications.Remove(otp);
            await _db.SaveChangesAsync();

            _metrics.RecordOtpVerification(TelemetryTags.Outcomes.Success);

            // Now issue the JWT — user is fully verified and logged in
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
        // RESEND OTP — POST /api/auth/resend-otp
        // ==========================================
        // Deletes any existing OTP for this user and sends a fresh 6-digit code.
        // Always returns 200 — we don't reveal whether the email is registered.
        [HttpPost("resend-otp")]
        [EnableRateLimiting("otp-send")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user != null && !user.EmailConfirmed)
                await SendOtpToUserAsync(user, OtpPurpose.EmailVerification);

            return Ok(new { message = "If that email is pending verification, a new code has been sent." });
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
            {
                _metrics.RecordLoginAttempt(TelemetryTags.Outcomes.Failure);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // CheckPasswordSignInAsync does ALL of this:
            // 1. Retrieves the stored password hash from the database
            // 2. Hashes the provided password with the same salt
            // 3. Compares the two hashes (never compares plain text passwords!)
            // 4. If lockoutOnFailure is true, tracks failed attempts
            // 5. If max attempts reached, locks the account for the configured duration
            var result = await _signInManager.CheckPasswordSignInAsync(
                user, dto.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                _metrics.RecordLoginAttempt(TelemetryTags.Outcomes.LockedOut);
                return StatusCode(423, new { message = "Account locked. Try again later." });
            }

            if (!result.Succeeded)
            {
                _metrics.RecordLoginAttempt(TelemetryTags.Outcomes.Failure);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Block access until the user has verified their email.
            // Send a fresh OTP in case the old one expired, then tell the
            // client to redirect to the OTP verification page.
            if (!user.EmailConfirmed)
            {
                await SendOtpToUserAsync(user, OtpPurpose.EmailVerification);
                _metrics.RecordLoginAttempt(TelemetryTags.Outcomes.Unverified);
                return StatusCode(403, new { requiresVerification = true, email = user.Email });
            }

            _metrics.RecordLoginAttempt(TelemetryTags.Outcomes.Success);

            // Password correct and email verified — generate token
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
        // FORGOT PASSWORD — POST /api/auth/forgot-password
        // ==========================================
        // Sends a 6-digit OTP to the user's email so they can reset their password
        // entirely within the MAUI app — no deep links or browser redirects needed.
        // Always returns 200 — we never reveal whether an email is registered.
        [HttpPost("forgot-password")]
        [EnableRateLimiting("otp-send")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);

            // If the account exists but email is not yet verified, tell the user
            // so they can go verify first rather than silently receiving nothing.
            if (user != null && !user.EmailConfirmed)
                return BadRequest(new
                {
                    requiresVerification = true,
                    email = user.Email,
                    message = "Your email address hasn't been verified yet. Please verify it first before resetting your password."
                });

            // Only send the code if the account exists and email is verified.
            if (user != null)
            {
                await SendOtpToUserAsync(user, OtpPurpose.PasswordReset);
                _metrics.RecordPasswordResetRequested();
            }

            return Ok(new { message = "If that email is registered, a reset code has been sent." });
        }


        // ==========================================
        // RESET PASSWORD — POST /api/auth/reset-password
        // ==========================================
        // The user enters the 6-digit OTP from their email plus a new password.
        // We validate the OTP (hash + expiry + purpose), then use Identity's
        // ResetPasswordAsync internally — the user never sees the Identity token.
        [HttpPost("reset-password")]
        [EnableRateLimiting("otp-verify")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return BadRequest(new { message = "Invalid request." });

            var otp = await _db.OtpVerifications
                .Where(o => o.UserId == user.Id && o.Purpose == OtpPurpose.PasswordReset)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            // Verify the submitted code against the stored BCrypt hash
            if (otp == null || otp.ExpiresAt < DateTime.UtcNow || !OtpHasher.Verify(dto.Code, otp.Code))
                return BadRequest(new { message = "Invalid or expired code." });

            // Clean up the OTP — single-use
            _db.OtpVerifications.Remove(otp);
            await _db.SaveChangesAsync();

            // Generate an Identity reset token internally and use it immediately.
            // The user authenticated via OTP; they never see the Identity token.
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            return Ok(new { message = "Password has been reset successfully." });
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


        // ==========================================
        // PRIVATE HELPER
        // ==========================================
        // Removes any existing OTPs for the user+purpose, generates a fresh 6-digit code,
        // hashes it with HMAC-SHA256 before storing, persists with a 10-minute expiry,
        // and sends the appropriate email.
        // Called from Register, Login (unverified), ResendOtp, and ForgotPassword.
        private async Task SendOtpToUserAsync(ApplicationUser user, string purpose)
        {
            // Remove stale OTPs for this purpose so only one active code exists at a time.
            // OTPs for other purposes (e.g. a pending email-verify code) are left untouched.
            var existing = _db.OtpVerifications.Where(o => o.UserId == user.Id && o.Purpose == purpose);
            _db.OtpVerifications.RemoveRange(existing);

            // 6-digit code: 100000–999999
            var code = Random.Shared.Next(100_000, 1_000_000).ToString();

            // Store the BCrypt hash — never plain text. BCrypt generates a unique salt automatically.
            var hashedCode = OtpHasher.Hash(code);

            _db.OtpVerifications.Add(new OtpVerification
            {
                UserId = user.Id,
                Code = hashedCode,
                Purpose = purpose,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            });

            await _db.SaveChangesAsync();

            // Send the plain code by email — only the hash is ever stored
            if (purpose == OtpPurpose.PasswordReset)
                await _emailService.SendPasswordResetOtpAsync(user.Email!, code);
            else
                await _emailService.SendOtpAsync(user.Email!, code);
        }
    }
}
