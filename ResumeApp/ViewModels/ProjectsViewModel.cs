using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Models;
using ResumeApp.Services;
using System.Collections.ObjectModel;

namespace ResumeApp.ViewModels;

public partial class ProjectsViewModel : ObservableObject
{
    private readonly ILocalStorageService _localStorageService;
    private readonly ISyncCoordinator _syncCoordinator;

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

    public ProjectsViewModel(ILocalStorageService localStorageService, ISyncCoordinator syncCoordinator)
    {
        _localStorageService = localStorageService;
        _syncCoordinator = syncCoordinator;
        _ = LoadEntriesAsync();
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

        var existing = ProjectEntries.FirstOrDefault(x => x.Id == _editingProjectId);
        var entry = new ProjectEntry
        {
            Id = existing?.Id ?? Guid.NewGuid().ToString(),
            UpdatedAt = existing?.UpdatedAt,
            Version = existing?.Version,
            Deleted = false,
            Name = CurrentProject.Name.Trim(),
            ProjectType = CurrentProject.ProjectType,
            Description = CurrentProject.Description.Trim(),
            Technologies = CurrentProject.Technologies,
            ProjectUrl = CurrentProject.ProjectUrl,
            StartDate = CurrentProject.StartDate,
            EndDate = CurrentProject.EndDate,
            ImagePaths = [.. CurrentProject.ImagePaths]
        };

        await _localStorageService.SaveItemAsync(entry);
        await RefreshEntriesAsync();
        _ = _syncCoordinator.SyncProjectsAsync();
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
            UpdatedAt = entry.UpdatedAt,
            Version = entry.Version,
            Deleted = entry.Deleted,
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
        if (entry is null)
        {
            return;
        }

        await _localStorageService.DeleteItemAsync(entry);
        await RefreshEntriesAsync();
        _ = _syncCoordinator.SyncProjectsAsync();
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

            await _syncCoordinator.SyncProjectsAsync();
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

    private async Task LoadEntriesAsync()
    {
        await _localStorageService.InitializeAsync();
        await RefreshEntriesAsync();
        _ = Task.Run(async () =>
        {
            await _syncCoordinator.SyncProjectsAsync();
            await MainThread.InvokeOnMainThreadAsync(RefreshEntriesAsync);
        });
    }

    private async Task RefreshEntriesAsync()
    {
        var entries = await _localStorageService.LoadItemsAsync<ProjectEntry>();
        ProjectEntries = new ObservableCollection<ProjectEntry>(entries);
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

    private void ResetEditor()
    {
        _editingProjectId = null;
        IsEditing = false;
        OnPropertyChanged(nameof(SubmitButtonText));
        CurrentProject = new ProjectEntry();
    }
}
