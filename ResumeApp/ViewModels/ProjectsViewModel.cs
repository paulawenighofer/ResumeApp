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

    private ProjectEntry _currentProject = new();
    private ObservableCollection<ProjectEntry> _projectEntries = [];
    private bool _isBusy;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isEditing;
    private bool _hasUnsavedChanges;

    private string? _editingProjectId;

    public ProjectEntry CurrentProject
    {
        get => _currentProject;
        set
        {
            if (SetProperty(ref _currentProject, value))
            {
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public ObservableCollection<ProjectEntry> ProjectEntries
    {
        get => _projectEntries;
        set => SetProperty(ref _projectEntries, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(SubmitButtonText));
            }
        }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set
        {
            if (SetProperty(ref _hasUnsavedChanges, value))
            {
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public string SubmitButtonText => IsEditing ? "Save changes" : "Add project";

    public bool CanSave => !IsBusy && (HasUnsavedChanges || HasPendingProjectInput());

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
            EndDate = CurrentProject.EndDate
        };

        if (IsEditing && _editingProjectId is not null)
        {
            ReplaceProject(entry);
        }
        else
        {
            ProjectEntries.Add(entry);
        }

        HasUnsavedChanges = true;

        await _localStorageService.SaveProjectsDraftAsync(ProjectEntries.ToList());
        if (!await SyncProjectAsync(entry))
        {
            ShowError("Project saved locally. Backend sync failed — please try again.");
        }
        else
        {
            await ShowToastAsync("Project saved");
        }
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
            EndDate = entry.EndDate
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

        if (!await ConfirmDeleteAsync("Delete project", "Delete this project?"))
        {
            return;
        }

        ProjectEntries.Remove(entry);
        HasUnsavedChanges = true;
        await _localStorageService.SaveProjectsDraftAsync(ProjectEntries.ToList());
        if (!await _apiService.DeleteProjectAsync(entry.Id))
        {
            ShowError("Project removed locally. Backend delete failed — please try again.");
        }
        else
        {
            await ShowToastAsync("Project deleted");
        }
        if (_editingProjectId == entry.Id)
        {
            ResetEditor();
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

            }

            if (!syncFailed)
            {
                await _localStorageService.ClearProjectsDraftAsync();
                HasUnsavedChanges = false;
            }

            if (syncFailed)
            {
                ShowError("Projects were saved locally, but the backend sync failed. You can continue and sync again later.");
            }

            await Shell.Current.GoToAsync("..");
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
            HasUnsavedChanges = true;
        }

        var entries = await _apiService.GetProjectsAsync();
        if (entries.Count > 0)
        {
            ProjectEntries = new ObservableCollection<ProjectEntry>(entries);
            HasUnsavedChanges = false;
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
        _ = ShowToastAsync(message, isError: true);
    }

    private void ResetError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    private async Task<bool> SyncProjectAsync(ProjectEntry project)
    {
        var success = int.TryParse(project.Id, out _)
            ? await _apiService.UpdateProjectAsync(project)
            : await _apiService.PostProjectAsync(project);

        if (success)
        {
            await _localStorageService.SaveProjectsDraftAsync(ProjectEntries.ToList());
        }

        return success;
    }

    [RelayCommand]
    private void MarkDirty() => HasUnsavedChanges = true;

    private static Task ShowToastAsync(string message, bool isError = false)
        => ToastService.ShowAsync(message, isError);

    private static Task<bool> ConfirmDeleteAsync(string title, string message)
        => App.Current?.MainPage?.DisplayAlert(title, message, "Delete", "Cancel")
           ?? Task.FromResult(true);

    private bool HasPendingProjectInput()
        => !string.IsNullOrWhiteSpace(CurrentProject.Name)
        || !string.IsNullOrWhiteSpace(CurrentProject.Description)
        || !string.IsNullOrWhiteSpace(CurrentProject.Technologies)
        || !string.IsNullOrWhiteSpace(CurrentProject.ProjectUrl);

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
