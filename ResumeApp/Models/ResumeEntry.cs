using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeApp.Models
{
    public class ResumeEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = "";
        public string JobTitle { get; set; } = "";
        public string JobDescription { get; set; } = "";
        public string CompanyDescription { get; set; } = "";
        public string Summary { get; set; } = "";
        public List<string> BulletPoints { get; set; } = new();
        public string? PdfPath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsGenerated { get; set; }
    }
}
