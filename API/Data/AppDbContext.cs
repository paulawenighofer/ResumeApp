using API.Models.Sync;
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
        public DbSet<SyncEducation> SyncEducations => Set<SyncEducation>();
        public DbSet<SyncExperience> SyncExperiences => Set<SyncExperience>();
        public DbSet<SyncSkill> SyncSkills => Set<SyncSkill>();
        public DbSet<SyncProject> SyncProjects => Set<SyncProject>();
        public DbSet<SyncCertification> SyncCertifications => Set<SyncCertification>();
        public DbSet<SyncResume> SyncResumes => Set<SyncResume>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Education>().HasOne<ApplicationUser>().WithMany(u => u.Educations).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Experience>().HasOne<ApplicationUser>().WithMany(u => u.Experiences).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Skill>().HasOne<ApplicationUser>().WithMany(u => u.Skills).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Project>().HasOne<ApplicationUser>().WithMany(u => u.Projects).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Certification>().HasOne<ApplicationUser>().WithMany(u => u.Certifications).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Resume>().HasOne<ApplicationUser>().WithMany(u => u.Resumes).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<OtpVerification>().HasOne<ApplicationUser>().WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<SyncEducation>().ToTable("SyncEducations");
            builder.Entity<SyncExperience>().ToTable("SyncExperiences");
            builder.Entity<SyncSkill>().ToTable("SyncSkills");
            builder.Entity<SyncProject>().ToTable("SyncProjects");
            builder.Entity<SyncCertification>().ToTable("SyncCertifications");
            builder.Entity<SyncResume>().ToTable("SyncResumes");
        }
    }
}
