using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Datasync.Client.Offline;
using ResumeApp.Data;
using ResumeApp.Models;

namespace ResumeApp.Services;

public sealed class SyncCoordinator : ISyncCoordinator
{
    private readonly IDbContextFactory<OfflineAppDbContext> _contextFactory;
    private readonly IApiService _apiService;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;

    public SyncCoordinator(IDbContextFactory<OfflineAppDbContext> contextFactory, IApiService apiService)
    {
        _contextFactory = contextFactory;
        _apiService = apiService;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        _initialized = true;
    }

    public Task SyncAllAsync() => WithLock(async () =>
    {
        if (!IsOnline())
        {
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.PushAsync(AllEntityTypes, new PushOptions());
        await UploadPendingFilesAsync(context);
        await context.PullAsync(AllEntityTypes, new PullOptions());
    });

    public Task SyncEducationAsync() => SyncEntitiesAsync([typeof(EducationEntry)]);
    public Task SyncExperienceAsync() => SyncEntitiesAsync([typeof(ExperienceEntry)]);
    public Task SyncSkillsAsync() => SyncEntitiesAsync([typeof(SkillEntry)]);
    public Task SyncProjectsAsync() => SyncEntitiesAsync([typeof(ProjectEntry)]);
    public Task SyncCertificationsAsync() => SyncEntitiesAsync([typeof(CertificationEntry)]);
    public Task SyncResumesAsync() => SyncEntitiesAsync([typeof(StoredResumeEntry)]);

    private Task SyncEntitiesAsync(IReadOnlyCollection<Type> entityTypes) => WithLock(async () =>
    {
        if (!IsOnline())
        {
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.PushAsync([.. entityTypes], new PushOptions());
        await UploadPendingFilesAsync(context);
        await context.PullAsync([.. entityTypes], new PullOptions());
    });

    private async Task UploadPendingFilesAsync(OfflineAppDbContext context)
    {
        var pendingProjects = await context.Projects
            .Where(x => !x.Deleted && x.ImagePathsJson != "[]")
            .ToListAsync();

        foreach (var project in pendingProjects.Where(x => x.ImagePaths.Count > 0))
        {
            await _apiService.UploadProjectImagesAsync(project.Id, project.ImagePaths);
            project.ImagePaths = [];
        }

        var pendingResumes = await context.Resumes
            .Where(x => !x.Deleted && x.LocalFilePath != null)
            .ToListAsync();

        foreach (var resume in pendingResumes.Where(x => !string.IsNullOrWhiteSpace(x.LocalFilePath)))
        {
            var uploadedUrl = await _apiService.UploadResumeFileAsync(resume.Id, resume.LocalFilePath!);
            if (!string.IsNullOrWhiteSpace(uploadedUrl))
            {
                resume.PdfBlobUrl = uploadedUrl;
                resume.LocalFilePath = null;
            }
        }

        await context.SaveChangesAsync();
        await context.PushAsync([typeof(ProjectEntry), typeof(StoredResumeEntry)], new PushOptions());
    }

    private static readonly Type[] AllEntityTypes =
    [
        typeof(EducationEntry),
        typeof(ExperienceEntry),
        typeof(SkillEntry),
        typeof(ProjectEntry),
        typeof(CertificationEntry),
        typeof(StoredResumeEntry)
    ];

    private static bool IsOnline() => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    private async Task WithLock(Func<Task> action)
    {
        await _gate.WaitAsync();
        try
        {
            await action();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
        {
            await SyncAllAsync();
        }
    }
}
