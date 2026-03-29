using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Models;
using ResumeApp.Services;
using System.Collections.ObjectModel;

namespace ResumeApp.ViewModels;

public partial class ProjectsViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly ILocalStorageService _localStorageService;

    [ObservableProperty]
    private ProjectEntry currentProject = new();

    [ObservableProperty]
    private ObservableCollection<ProjectEntry> projectEntries = [];

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isEditing;

    private string? _editingProjectId;

    public string SubmitButtonText => IsEditing ? "Save changes" : "Add project";

    public IList<string> ProjectTypes { get; } =
    [
        "Personal Project",
        "Professional Project",
        "Open Source",
        "School Project",
        "University Project",
        "Freelance"
    ];

    public ProjectsViewModel(IApiService apiService, ILocalStorageService localStorageService)
    {
        _apiService = apiService;
        _localStorageService = localStorageService;
        _ = LoadDraftsAndEntriesAsync();
    }

    [RelayCommand]
    private async Task AddProject()
    {
        ResetError();

        if (string.IsNullOrWhiteSpace(CurrentProject.Name) ||
            string.IsNullOrWhiteSpace(CurrentProject.Description))
        {
            ShowError("Please fill in the project name and description.");
            return;
        }

        if (CurrentProject.EndDate < CurrentProject.StartDate)
        {
            ShowError("End date must be after the start date.");
            return;
        }

        var entry = new ProjectEntry
        {
            Id = _editingProjectId ?? Guid.NewGuid().ToString(),
            Name = CurrentProject.Name.Trim(),
            ProjectType = CurrentProject.ProjectType,
            Description = CurrentProject.Description.Trim(),
            Technologies = CurrentProject.Technologies,
            ProjectUrl = CurrentProject.ProjectUrl,
            StartDate = CurrentProject.StartDate,
            EndDate = CurrentProject.EndDate,
            ImagePaths = [.. CurrentProject.ImagePaths]
        };

        if (IsEditing && _editingProjectId is not null)
        {
            ReplaceProject(entry);
        }
        else
        {
            ProjectEntries.Add(entry);
        }

        await _localStorageService.SaveProjectsDraftAsync(ProjectEntries.ToList());
        ResetEditor();
    }

    [RelayCommand]
    private void EditProject(ProjectEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        _editingProjectId = entry.Id;
        IsEditing = true;
        OnPropertyChanged(nameof(SubmitButtonText));
        CurrentProject = new ProjectEntry
        {
            Id = entry.Id,
            Name = entry.Name,
            ProjectType = entry.ProjectType,
            Description = entry.Description,
            Technologies = entry.Technologies,
            ProjectUrl = entry.ProjectUrl,
            StartDate = entry.StartDate,
            EndDate = entry.EndDate,
            ImagePaths = [.. entry.ImagePaths]
        };
    }

    [RelayCommand]
    private void CancelEdit() => ResetEditor();

    [RelayCommand]
    private async Task DeleteProject(ProjectEntry? entry)
    {
        if (entry is null || !ProjectEntries.Contains(entry))
        {
            return;
        }

        ProjectEntries.Remove(entry);
        await _localStorageService.SaveProjectsDraftAsync(ProjectEntries.ToList());
        if (_editingProjectId == entry.Id)
        {
            ResetEditor();
        }
    }

    [RelayCommand]
    private async Task SelectProjectImages()
    {
        ResetError();

        try
        {
            var results = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Select project images"
            });

            if (results is null)
            {
                return;
            }

            CurrentProject.ImagePaths = results
                .Where(file => !string.IsNullOrWhiteSpace(file.FullPath))
                .Select(file => file.FullPath)
                .ToList()!;
        }
        catch (Exception ex)
        {
            ShowError($"Could not select images: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveAndContinue()
    {
        try
        {
            IsBusy = true;
            ResetError();

            if (HasPendingProjectInput())
            {
                var added = await TryAddCurrentProjectAsDraftAsync();
                if (!added)
                {
                    return;
                }
            }

            if (ProjectEntries.Count == 0)
            {
                ShowError("Please add at least one project.");
                return;
            }

            await _localStorageService.SaveProjectsDraftAsync(ProjectEntries.ToList());

            var syncFailed = false;

            foreach (var project in ProjectEntries)
            {
                var success = int.TryParse(project.Id, out _)
                    ? await _apiService.UpdateProjectAsync(project)
                    : await _apiService.PostProjectAsync(project);

                if (!success)
                {
                    syncFailed = true;
                    break;
                }

                if (project.ImagePaths.Count > 0)
                {
                    await _apiService.UploadProjectImagesAsync(project.Id, project.ImagePaths);
                }
            }

            if (!syncFailed)
            {
                await _localStorageService.ClearProjectsDraftAsync();
            }

            if (syncFailed)
            {
                ShowError("Projects were saved locally, but the backend sync failed. You can continue and sync again later.");
            }

            await Shell.Current.GoToAsync("//main");
        }
        catch (Exception ex)
        {
            ShowError($"Error while saving: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadDraftsAndEntriesAsync()
    {
        var drafts = await _localStorageService.LoadProjectsDraftAsync();
        if (drafts.Count > 0)
        {
            ProjectEntries = new ObservableCollection<ProjectEntry>(drafts);
        }

        var entries = await _apiService.GetProjectsAsync();
        if (entries.Count > 0)
        {
            ProjectEntries = new ObservableCollection<ProjectEntry>(entries);
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ResetError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    partial void OnIsEditingChanged(bool value) => OnPropertyChanged(nameof(SubmitButtonText));

    private bool HasPendingProjectInput()
        => !string.IsNullOrWhiteSpace(CurrentProject.Name)
        || !string.IsNullOrWhiteSpace(CurrentProject.Description)
        || !string.IsNullOrWhiteSpace(CurrentProject.Technologies)
        || !string.IsNullOrWhiteSpace(CurrentProject.ProjectUrl)
        || CurrentProject.ImagePaths.Count > 0;

    private async Task<bool> TryAddCurrentProjectAsDraftAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentProject.Name) ||
            string.IsNullOrWhiteSpace(CurrentProject.Description))
        {
            ShowError("Please complete the current project form or clear it before continuing.");
            return false;
        }

        if (CurrentProject.EndDate < CurrentProject.StartDate)
        {
            ShowError("End date must be after the start date.");
            return false;
        }

        await AddProject();
        return !HasError;
    }

    private void ReplaceProject(ProjectEntry entry)
    {
        var index = ProjectEntries
            .Select((item, idx) => new { item, idx })
            .FirstOrDefault(x => x.item.Id == entry.Id)?.idx ?? -1;

        if (index >= 0)
        {
            ProjectEntries[index] = entry;
        }
    }

    private void ResetEditor()
    {
        _editingProjectId = null;
        IsEditing = false;
        OnPropertyChanged(nameof(SubmitButtonText));
        CurrentProject = new ProjectEntry();
    }
}
