using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Models;
using ResumeApp.Services;
using System.Collections.ObjectModel;

namespace ResumeApp.ViewModels;

public partial class ExperienceViewModel : ObservableObject
{
    private readonly ILocalStorageService _localStorageService;
    private readonly ISyncCoordinator _syncCoordinator;

    [ObservableProperty]
    private ExperienceEntry currentExperience = new();

    [ObservableProperty]
    private ObservableCollection<ExperienceEntry> experienceEntries = [];

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isEditing;

    private string? _editingExperienceId;

    public string SubmitButtonText => IsEditing ? "Save changes" : "Add entry";

    public IList<string> EmploymentTypes { get; } =
    [
        "Full-time",
        "Part-time",
        "Freelance",
        "Internship",
        "Temporary"
    ];

    public ExperienceViewModel(ILocalStorageService localStorageService, ISyncCoordinator syncCoordinator)
    {
        _localStorageService = localStorageService;
        _syncCoordinator = syncCoordinator;
        _ = LoadEntriesAsync();
    }

    [RelayCommand]
    private async Task AddExperience()
    {
        ResetError();

        if (string.IsNullOrWhiteSpace(CurrentExperience.Company) ||
            string.IsNullOrWhiteSpace(CurrentExperience.JobTitle) ||
            string.IsNullOrWhiteSpace(CurrentExperience.Description))
        {
            ShowError("Please fill in the company, role, and description.");
            return;
        }

        if (!CurrentExperience.IsCurrentJob && CurrentExperience.EndDate < CurrentExperience.StartDate)
        {
            ShowError("End date must be after the start date.");
            return;
        }

        var existing = ExperienceEntries.FirstOrDefault(x => x.Id == _editingExperienceId);
        var entry = new ExperienceEntry
        {
            Id = existing?.Id ?? Guid.NewGuid().ToString(),
            UpdatedAt = existing?.UpdatedAt,
            Version = existing?.Version,
            Deleted = false,
            Company = CurrentExperience.Company.Trim(),
            JobTitle = CurrentExperience.JobTitle.Trim(),
            EmploymentType = CurrentExperience.EmploymentType,
            Location = CurrentExperience.Location.Trim(),
            StartDate = CurrentExperience.StartDate,
            EndDate = CurrentExperience.EndDate,
            IsCurrentJob = CurrentExperience.IsCurrentJob,
            Description = CurrentExperience.Description.Trim(),
            Technologies = CurrentExperience.Technologies
        };

        await _localStorageService.SaveItemAsync(entry);
        await RefreshEntriesAsync();
        _ = _syncCoordinator.SyncExperienceAsync();
        ResetEditor();
    }

    [RelayCommand]
    private void EditExperience(ExperienceEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        _editingExperienceId = entry.Id;
        IsEditing = true;
        OnPropertyChanged(nameof(SubmitButtonText));
        CurrentExperience = new ExperienceEntry
        {
            Id = entry.Id,
            UpdatedAt = entry.UpdatedAt,
            Version = entry.Version,
            Deleted = entry.Deleted,
            Company = entry.Company,
            JobTitle = entry.JobTitle,
            EmploymentType = entry.EmploymentType,
            Location = entry.Location,
            StartDate = entry.StartDate,
            EndDate = entry.EndDate,
            IsCurrentJob = entry.IsCurrentJob,
            Description = entry.Description,
            Technologies = entry.Technologies
        };
    }

    [RelayCommand]
    private void CancelEdit() => ResetEditor();

    [RelayCommand]
    private async Task DeleteExperience(ExperienceEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        await _localStorageService.DeleteItemAsync(entry);
        await RefreshEntriesAsync();
        _ = _syncCoordinator.SyncExperienceAsync();
        if (_editingExperienceId == entry.Id)
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

            if (HasPendingExperienceInput())
            {
                var added = await TryAddCurrentExperienceAsDraftAsync();
                if (!added)
                {
                    return;
                }
            }

            if (ExperienceEntries.Count == 0)
            {
                ShowError("Please add at least one experience entry.");
                return;
            }

            await _syncCoordinator.SyncExperienceAsync();
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
            await _syncCoordinator.SyncExperienceAsync();
            await MainThread.InvokeOnMainThreadAsync(RefreshEntriesAsync);
        });
    }

    private async Task RefreshEntriesAsync()
    {
        var entries = await _localStorageService.LoadItemsAsync<ExperienceEntry>();
        ExperienceEntries = new ObservableCollection<ExperienceEntry>(entries);
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

    private bool HasPendingExperienceInput()
        => !string.IsNullOrWhiteSpace(CurrentExperience.Company)
        || !string.IsNullOrWhiteSpace(CurrentExperience.JobTitle)
        || !string.IsNullOrWhiteSpace(CurrentExperience.Location)
        || !string.IsNullOrWhiteSpace(CurrentExperience.Description)
        || !string.IsNullOrWhiteSpace(CurrentExperience.Technologies);

    private async Task<bool> TryAddCurrentExperienceAsDraftAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentExperience.Company) ||
            string.IsNullOrWhiteSpace(CurrentExperience.JobTitle) ||
            string.IsNullOrWhiteSpace(CurrentExperience.Description))
        {
            ShowError("Please complete the current experience form or clear it before continuing.");
            return false;
        }

        if (!CurrentExperience.IsCurrentJob && CurrentExperience.EndDate < CurrentExperience.StartDate)
        {
            ShowError("End date must be after the start date.");
            return false;
        }

        await AddExperience();
        return !HasError;
    }

    private void ResetEditor()
    {
        _editingExperienceId = null;
        IsEditing = false;
        OnPropertyChanged(nameof(SubmitButtonText));
        CurrentExperience = new ExperienceEntry();
    }
}
