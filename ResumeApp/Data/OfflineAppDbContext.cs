using CommunityToolkit.Datasync.Client.Authentication;
using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Offline;
using Microsoft.EntityFrameworkCore;
using ResumeApp.Models;
using ResumeApp.Services;

namespace ResumeApp.Data;

public class OfflineAppDbContext : OfflineDbContext
{
    private readonly HttpClient _httpClient;
    private readonly DatasyncAuthProvider _authProvider;

    public OfflineAppDbContext(
        DbContextOptions<OfflineAppDbContext> options,
        HttpClient httpClient,
        DatasyncAuthProvider authProvider) : base(options)
    {
        _httpClient = httpClient;
        _authProvider = authProvider;
    }

    public DbSet<EducationEntry> Educations => Set<EducationEntry>();
    public DbSet<ExperienceEntry> Experiences => Set<ExperienceEntry>();
    public DbSet<SkillEntry> Skills => Set<SkillEntry>();
    public DbSet<ProjectEntry> Projects => Set<ProjectEntry>();
    public DbSet<CertificationEntry> Certifications => Set<CertificationEntry>();
    public DbSet<StoredResumeEntry> Resumes => Set<StoredResumeEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<EducationEntry>().HasKey(x => x.Id);
        modelBuilder.Entity<ExperienceEntry>().HasKey(x => x.Id);
        modelBuilder.Entity<SkillEntry>().HasKey(x => x.Id);
        modelBuilder.Entity<ProjectEntry>().HasKey(x => x.Id);
        modelBuilder.Entity<CertificationEntry>().HasKey(x => x.Id);
        modelBuilder.Entity<StoredResumeEntry>().HasKey(x => x.Id);
    }

    protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseHttpClientOptions(new HttpClientOptions
        {
            Endpoint = _httpClient.BaseAddress!,
            HttpPipeline = [_authProvider]
        });

        optionsBuilder.UseDefaultConflictResolver(new ClientWinsConflictResolver());
        optionsBuilder.Entity<EducationEntry>(cfg => cfg.Endpoint = new Uri("/tables/educations", UriKind.Relative));
        optionsBuilder.Entity<ExperienceEntry>(cfg => cfg.Endpoint = new Uri("/tables/experiences", UriKind.Relative));
        optionsBuilder.Entity<SkillEntry>(cfg => cfg.Endpoint = new Uri("/tables/skills", UriKind.Relative));
        optionsBuilder.Entity<ProjectEntry>(cfg => cfg.Endpoint = new Uri("/tables/projects", UriKind.Relative));
        optionsBuilder.Entity<CertificationEntry>(cfg => cfg.Endpoint = new Uri("/tables/certifications", UriKind.Relative));
        optionsBuilder.Entity<StoredResumeEntry>(cfg => cfg.Endpoint = new Uri("/tables/resumes", UriKind.Relative));
    }
}
