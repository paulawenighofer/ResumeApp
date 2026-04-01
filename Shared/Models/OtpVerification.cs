using System.ComponentModel.DataAnnotations;

namespace Shared.Models
{
    public class OtpVerification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Distinguishes email-verification OTPs from password-reset OTPs so one
        // cannot be used in place of the other.
        public string Purpose { get; set; } = OtpPurpose.EmailVerification;
    }
}
