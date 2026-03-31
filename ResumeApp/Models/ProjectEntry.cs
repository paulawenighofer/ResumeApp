using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeApp.Models
{
    public class ProjectEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string ProjectType { get; set; } = "Personal Project";
        public string Description { get; set; } = "";
        public string? Technologies { get; set; }
        public string? ProjectUrl { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(3);
        public List<string> ImagePaths { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string DurationText
        {
            get
            {
                var duration = EndDate - StartDate;
                int months = (int)(duration.TotalDays / 30.44);
                return months > 0 ? $"{months} month{(months > 1 ? "s" : "")}" : "< 1 month";
            }
        }

        public List<string> TechList => Technologies?.Split(',')
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList() ?? new();

        public bool HasImages => ImagePaths?.Count > 0;
    }
}
