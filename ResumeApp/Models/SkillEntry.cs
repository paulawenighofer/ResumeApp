using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeApp.Models
{
    public class SkillEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string ProficiencyLevel { get; set; } = "Intermediate";
        public string Category { get; set; } = "Programming Language";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ProficiencyScore
        {
            get => ProficiencyLevel switch
            {
                "Beginner" => 1,
                "Intermediate" => 2,
                "Advanced" => 3,
                "Expert" => 4,
                _ => 0
            };
        }

    }
}
