using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace API.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Education> Educations => Set<Education>();
        public DbSet<Experience> Experiences => Set<Experience>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<Certification> Certifications => Set<Certification>();
        public DbSet<Resume> Resumes => Set<Resume>();
        public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Required for Identity tables

            // Since all your models follow the convention (UserId property
            // matching ApplicationUser's Id), EF Core can infer the relationships.
            // We only need to be explicit about cascade delete behavior:

            builder.Entity<Education>().HasOne<ApplicationUser>().WithMany(u => u.Educations).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Experience>().HasOne<ApplicationUser>().WithMany(u => u.Experiences).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Skill>().HasOne<ApplicationUser>().WithMany(u => u.Skills).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Project>().HasOne<ApplicationUser>().WithMany(u => u.Projects).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Certification>().HasOne<ApplicationUser>().WithMany(u => u.Certifications).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Resume>().HasOne<ApplicationUser>().WithMany(u => u.Resumes).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<OtpVerification>().HasOne<ApplicationUser>().WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
