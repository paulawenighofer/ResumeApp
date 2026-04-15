using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Models;
using ResumeApp.Services;
using System.Collections.ObjectModel;

namespace ResumeApp.ViewModels;

public partial class EducationViewModel : ObservableObject
{
    private readonly ILocalStorageService _localStorageService;
    private readonly ISyncCoordinator _syncCoordinator;

    [ObservableProperty]
    private EducationEntry currentEducation = new();

    [ObservableProperty]
    private ObservableCollection<EducationEntry> educationEntries = [];

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isEditing;

    private string? _editingEducationId;

    public string SubmitButtonText => IsEditing ? "Save changes" : "Add entry";

    public IList<string> DegreeOptions { get; } =
    [
        "High School Diploma",
        "Bachelor",
        "Master",
        "Diploma",
        "Doctorate",
        "Vocational Training",
        "Other"
    ];

    public EducationViewModel(ILocalStorageService localStorageService, ISyncCoordinator syncCoordinator)
    {
        _localStorageService = localStorageService;
        _syncCoordinator = syncCoordinator;
        _ = LoadEntriesAsync();
    }

    [RelayCommand]
    private async Task AddEducation()
    {
        ResetError();

        if (string.IsNullOrWhiteSpace(CurrentEducation.School) ||
            string.IsNullOrWhiteSpace(CurrentEducation.Degree) ||
            string.IsNullOrWhiteSpace(CurrentEducation.FieldOfStudy))
        {
            ShowError("Please fill in the school, degree, and field of study.");
            return;
        }

        if (CurrentEducation.EndDate < CurrentEducation.StartDate)
        {
            ShowError("End date must be after the start date.");
            return;
        }

        var existing = EducationEntries.FirstOrDefault(x => x.Id == _editingEducationId);
        var entry = new EducationEntry
        {
            Id = existing?.Id ?? Guid.NewGuid().ToString(),
            UpdatedAt = existing?.UpdatedAt,
            Version = existing?.Version,
            Deleted = false,
            School = CurrentEducation.School.Trim(),
            Degree = CurrentEducation.Degree,
            FieldOfStudy = CurrentEducation.FieldOfStudy.Trim(),
            StartDate = CurrentEducation.StartDate,
            EndDate = CurrentEducation.EndDate,
            GPA = CurrentEducation.GPA,
            Description = CurrentEducation.Description
        };

        await _localStorageService.SaveItemAsync(entry);
        await RefreshEntriesAsync();
        _ = _syncCoordinator.SyncEducationAsync();
        ResetEditor();
    }

    [RelayCommand]
    private void EditEducation(EducationEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        _editingEducationId = entry.Id;
        IsEditing = true;
        OnPropertyChanged(nameof(SubmitButtonText));
        CurrentEducation = new EducationEntry
        {
            Id = entry.Id,
            UpdatedAt = entry.UpdatedAt,
            Version = entry.Version,
            Deleted = entry.Deleted,
            School = entry.School,
            Degree = entry.Degree,
            FieldOfStudy = entry.FieldOfStudy,
            StartDate = entry.StartDate,
            EndDate = entry.EndDate,
            GPA = entry.GPA,
            Description = entry.Description
        };
    }

    [RelayCommand]
    private void CancelEdit() => ResetEditor();

    [RelayCommand]
    private async Task DeleteEducation(EducationEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        await _localStorageService.DeleteItemAsync(entry);
        await RefreshEntriesAsync();
        _ = _syncCoordinator.SyncEducationAsync();
        if (_editingEducationId == entry.Id)
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

            if (HasPendingEducationInput())
            {
                var added = await TryAddCurrentEducationAsDraftAsync();
                if (!added)
                {
                    return;
                }
            }

            if (EducationEntries.Count == 0)
            {
                ShowError("Please add at least one education entry.");
                return;
            }

            await _syncCoordinator.SyncEducationAsync();
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
            await _syncCoordinator.SyncEducationAsync();
            await MainThread.InvokeOnMainThreadAsync(RefreshEntriesAsync);
        });
    }

    private async Task RefreshEntriesAsync()
    {
        var entries = await _localStorageService.LoadItemsAsync<EducationEntry>();
        EducationEntries = new ObservableCollection<EducationEntry>(entries);
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

    private bool HasPendingEducationInput()
        => !string.IsNullOrWhiteSpace(CurrentEducation.School)
        || !string.IsNullOrWhiteSpace(CurrentEducation.Degree)
        || !string.IsNullOrWhiteSpace(CurrentEducation.FieldOfStudy)
        || !string.IsNullOrWhiteSpace(CurrentEducation.GPA)
        || !string.IsNullOrWhiteSpace(CurrentEducation.Description);

    private async Task<bool> TryAddCurrentEducationAsDraftAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentEducation.School) ||
            string.IsNullOrWhiteSpace(CurrentEducation.Degree) ||
            string.IsNullOrWhiteSpace(CurrentEducation.FieldOfStudy))
        {
            ShowError("Please complete the current education form or clear it before continuing.");
            return false;
        }

        if (CurrentEducation.EndDate < CurrentEducation.StartDate)
        {
            ShowError("End date must be after the start date.");
            return false;
        }

        await AddEducation();
        return !HasError;
    }

    private void ResetEditor()
    {
        _editingEducationId = null;
        IsEditing = false;
        OnPropertyChanged(nameof(SubmitButtonText));
        CurrentEducation = new EducationEntry();
    }
}
