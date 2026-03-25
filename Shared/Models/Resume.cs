using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class Resume
    {
        public int Id
        {
            get; set;
        }

        [Required]
        public string UserId { get; set; } = string.Empty;

        // What job this resume was tailored for
        [Required, MaxLength(200)]
        public string TargetJobTitle { get; set; } = string.Empty;    // "Software Engineer"

        [MaxLength(5000)]
        public string? JobDescription
        {
            get; set;
        }                    // the pasted job posting

        [MaxLength(2000)]
        public string? CompanyDescription
        {
            get; set;
        }                // about the company

        // AI-generated content — stored as JSON string.
        // Why JSON? Because the AI returns structured data (summary, bullets, skills)
        // and storing it as JSON means we can easily deserialize it in C#
        // without needing separate tables for each section.
        // 
        // Example JSON structure:
        // {
        //   "summary": "Experienced software engineer with...",
        //   "experience": [
        //     { "company": "Snow College IT", "bullets": ["Led...", "Built..."] }
        //   ],
        //   "highlightedSkills": ["C#", "Azure", "Docker"],
        //   "highlightedProjects": [...]
        // }
        [MaxLength(10000)]
        public string? GeneratedContent
        {
            get; set;
        }

        // URL to the exported PDF in Azure Blob Storage
        [MaxLength(500)]
        public string? PdfBlobUrl
        {
            get; set;
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt
        {
            get; set;
        }

    }
}
