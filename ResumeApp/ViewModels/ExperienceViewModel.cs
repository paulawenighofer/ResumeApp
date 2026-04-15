using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Models;
using ResumeApp.Services;
using ResumeApp.Views;
using System.Collections.ObjectModel;

namespace ResumeApp.ViewModels;

public partial class ExperienceViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly ILocalStorageService _localStorageService;

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

    public ExperienceViewModel(IApiService apiService, ILocalStorageService localStorageService)
    {
        _apiService = apiService;
        _localStorageService = localStorageService;
        _ = LoadDraftsAndEntriesAsync();
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

        var entry = new ExperienceEntry
        {
            Id = _editingExperienceId ?? Guid.NewGuid().ToString(),
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

        if (IsEditing && _editingExperienceId is not null)
        {
            ReplaceExperience(entry);
        }
        else
        {
            ExperienceEntries.Add(entry);
        }

        await _localStorageService.SaveExperienceDraftAsync(ExperienceEntries.ToList());
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
        if (entry is null || !ExperienceEntries.Contains(entry))
        {
            return;
        }

        ExperienceEntries.Remove(entry);
        await _localStorageService.SaveExperienceDraftAsync(ExperienceEntries.ToList());
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

            await _localStorageService.SaveExperienceDraftAsync(ExperienceEntries.ToList());

            var syncFailed = false;

            foreach (var entry in ExperienceEntries)
            {
                var success = int.TryParse(entry.Id, out _)
                    ? await _apiService.UpdateExperienceAsync(entry)
                    : await _apiService.PostExperienceAsync(entry);

                if (!success)
                {
                    syncFailed = true;
                    break;
                }
            }

            if (!syncFailed)
            {
                await _localStorageService.ClearExperienceDraftAsync();
            }

            if (syncFailed)
            {
                ShowError("Experience saved locally. Backend sync failed — please try again.");
                return;
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
        var drafts = await _localStorageService.LoadExperienceDraftAsync();
        if (drafts.Count > 0)
        {
            ExperienceEntries = new ObservableCollection<ExperienceEntry>(drafts);
        }

        var entries = await _apiService.GetExperienceAsync();
        if (entries.Count > 0)
        {
            ExperienceEntries = new ObservableCollection<ExperienceEntry>(entries);
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

    private void ReplaceExperience(ExperienceEntry entry)
    {
        var index = ExperienceEntries
            .Select((item, idx) => new { item, idx })
            .FirstOrDefault(x => x.item.Id == entry.Id)?.idx ?? -1;

        if (index >= 0)
        {
            ExperienceEntries[index] = entry;
        }
    }

    private void ResetEditor()
    {
        _editingExperienceId = null;
        IsEditing = false;
        OnPropertyChanged(nameof(SubmitButtonText));
        CurrentExperience = new ExperienceEntry();
    }
}
