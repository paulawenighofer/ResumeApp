using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class Project
    {
        public int Id
        {
            get; set;
        }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;         // "Hotel Management System"

        [MaxLength(2000)]
        public string? Description
        {
            get; set;
        }                  // what you built and why

        // Comma-separated or newline-separated list of technologies.
        // Example: "C#, ASP.NET Core, PostgreSQL, Docker"
        // Kept as a simple string for MVP — no need for a separate table.
        [MaxLength(500)]
        public string? TechnologiesUsed
        {
            get; set;
        }

        [MaxLength(500)]
        public string? ProjectUrl
        {
            get; set;
        }                   // GitHub link, live demo, etc.

        public DateTime? StartDate
        {
            get; set;
        }

        public DateTime? EndDate
        {
            get; set;
        }

    }
}
