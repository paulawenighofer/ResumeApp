using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models
{
    public class Education
    {
        // EF Core sees a property named "Id" and automatically makes it
        // the primary key with auto-increment.
        public int Id
        {
            get; set;
        }

        // This links the education record to a specific user.
        // EF Core sees "UserId" and knows it's a foreign key to ApplicationUser
        // because ApplicationUser's primary key is "Id" and this follows
        // the naming convention: {NavigationProperty}Id or {PrincipalClass}Id
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Institution { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Degree { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? FieldOfStudy
        {
            get; set;
        }

        public DateTime StartDate
        {
            get; set;
        }

        public DateTime? EndDate
        {
            get; set;
        }                    // null = currently enrolled

        public decimal? GPA
        {
            get; set;
        }                         // optional

        [MaxLength(1000)]
        public string? Description
        {
            get; set;
        }                  // honors, activities, etc.

        // Navigation property back to the user.
        // This lets you write: education.User.FirstName
        // EF Core uses this for JOIN queries.
    }
}
