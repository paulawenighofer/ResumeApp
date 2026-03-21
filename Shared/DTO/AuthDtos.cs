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
}
