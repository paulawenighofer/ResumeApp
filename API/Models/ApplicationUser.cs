using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Shared.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName
        {
            get; set;
        }
        public string? LastName
        {
            get; set;
        }
        public string? ProfileImageUrl
        {
            get; set;
        }

        // A short bio or professional summary the user writes about themselves.
        // The AI can use this as additional context when generating resumes.
        public string? Bio
        {
            get; set;
        }

        // Navigation properties
        public ICollection<Education> Educations { get; set; } = new List<Education>();
        public ICollection<Experience> Experiences { get; set; } = new List<Experience>();
        public ICollection<Skill> Skills { get; set; } = new List<Skill>();
        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<Certification> Certifications { get; set; } = new List<Certification>();
        public ICollection<Resume> Resumes { get; set; } = new List<Resume>();
    }
}
