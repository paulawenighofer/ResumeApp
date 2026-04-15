namespace ResumeApp.Services;

public interface ISyncCoordinator
{
    Task InitializeAsync();
    Task SyncAllAsync();
    Task SyncEducationAsync();
    Task SyncExperienceAsync();
    Task SyncSkillsAsync();
    Task SyncProjectsAsync();
    Task SyncCertificationsAsync();
    Task SyncResumesAsync();
}
