using System;

namespace ResumeApp.Models
{
    public class CertificationEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string IssuingOrganization { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; } = DateTime.Now;
        public DateTime ExpirationDate { get; set; } = DateTime.Now.AddYears(1);
        public string? CredentialId { get; set; }
        public string? CredentialUrl { get; set; }

        public string ValidityText
        {
            get
            {
                var issuedText = IssueDate.ToString("MMM yyyy");
                var expiresText = ExpirationDate.ToString("MMM yyyy");
                return $"Issued {issuedText} • Expires {expiresText}";
            }
        }
    }
}
