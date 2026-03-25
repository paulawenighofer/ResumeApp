using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class Skill
    {
        public int Id
        {
            get; set;
        }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;          // "C#", "JavaScript", "Project Management"

        // Categorize skills so the AI can pick the most relevant ones.
        // Examples: "Programming Language", "Framework", "Tool", "Soft Skill"
        [MaxLength(100)]
        public string? Category
        {
            get; set;
        }

        // Optional: let users rate their own proficiency.
        // 1 = Beginner, 2 = Intermediate, 3 = Advanced, 4 = Expert
        // The AI can use this to decide which skills to highlight.
        public int? ProficiencyLevel
        {
            get; set;
        }

    }
}
