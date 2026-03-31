using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeApp.Models
{
    public class EducationEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string School { get; set; } = "";
        public string Degree { get; set; } = "";
        public string FieldOfStudy { get; set; } = "";
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddYears(1);
        public string? GPA { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string DurationText
        {
            get
            {
                var duration = EndDate - StartDate;
                int years = (int)(duration.TotalDays / 365.25);
                return years > 0 ? $"{years} year{(years > 1 ? "s" : "")}" : "< 1 year";
            }
        }
    }
}
