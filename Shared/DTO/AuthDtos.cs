using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTO
{
    public class RegisterDto
    {
        [Required]                    // If missing, ASP.NET returns 400 Bad Request automatically
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]      // [EmailAddress] validates format (must have @ and domain)
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8)]      // Must be at least 8 chars (matches our Identity config)
        public string Password { get; set; } = string.Empty;
    }

    // What the mobile app sends when logging in
    public class LoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }


    // What the backend sends back after any successful login/register
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;      // The JWT token
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    // Returned after registration — no token yet, OTP sent to email instead
    public class RegisterPendingResponseDto
    {
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    // Sent when the user submits their OTP code
    public class VerifyOtpDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;
    }

    // Sent to request a new OTP (resend)
    public class ResendOtpDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    // Sent when requesting a password reset email
    public class ForgotPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    // Sent when submitting the reset code + new password
    public class ResetPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        // The 6-digit OTP from the password-reset email
        [Required, StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
