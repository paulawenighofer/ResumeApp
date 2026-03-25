using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class Certification
    {
        public int Id
        {
            get; set;
        }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Name { get; set; } = string.Empty;          // "AWS Certified Cloud Practitioner"

        [Required, MaxLength(200)]
        public string IssuingOrganization { get; set; } = string.Empty;  // "Amazon Web Services"

        public DateTime? IssueDate
        {
            get; set;
        }

        public DateTime? ExpirationDate
        {
            get; set;
        }             // null = doesn't expire

        // The credential ID or verification URL, so employers can verify it
        [MaxLength(500)]
        public string? CredentialId
        {
            get; set;
        }

        [MaxLength(500)]
        public string? CredentialUrl
        {
            get; set;
        }

    }
}
