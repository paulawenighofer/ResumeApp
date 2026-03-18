using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class Experience
    {
        public int Id
        {
            get; set;
        }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Company { get; set; } = string.Empty;       // "Snow College IT"

        [Required, MaxLength(200)]
        public string JobTitle { get; set; } = string.Empty;      // "IT Helpdesk Technician"

        [MaxLength(200)]
        public string? Location
        {
            get; set;
        }                     // "Ephraim, UT"

        public DateTime StartDate
        {
            get; set;
        }

        public DateTime? EndDate
        {
            get; set;
        }                    // null = current job

        public bool IsCurrentJob
        {
            get; set;
        }

        // Store bullet points as a single text block separated by newlines.
        // When the AI generates tailored bullets, they'll replace or augment these.
        // We use a simple string here instead of a separate BulletPoint table
        // to keep the MVP simple. Each bullet is separated by \n
        [MaxLength(5000)]
        public string? Responsibilities
        {
            get; set;
        }

    }
}
