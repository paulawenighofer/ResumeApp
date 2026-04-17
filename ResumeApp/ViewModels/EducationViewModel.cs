using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Services;
using ResumeApp.Models;
using ResumeApp.Services;
using ResumeApp.Views;
using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;

namespace ResumeApp.ViewModels;

public partial class EducationViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly ILocalStorageService _localStorageService;


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

    [ObservableProperty]
    private bool hasUnsavedChanges;

    private string? _editingEducationId;

    public string SubmitButtonText => IsEditing ? "Save changes" : "Add entry";

    public bool CanSave => !IsBusy && (HasUnsavedChanges || HasPendingEducationInput());

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

    public EducationViewModel(IApiService apiService, ILocalStorageService localStorageService)
    {
        _apiService = apiService;
        _localStorageService = localStorageService;
        _ = LoadDraftsAndEntriesAsync();
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

        var entry = new EducationEntry
        {
            Id = _editingEducationId ?? Guid.NewGuid().ToString(),
            School = CurrentEducation.School.Trim(),
            Degree = CurrentEducation.Degree,
            FieldOfStudy = CurrentEducation.FieldOfStudy.Trim(),
            StartDate = CurrentEducation.StartDate,
            EndDate = CurrentEducation.EndDate,
            GPA = CurrentEducation.GPA,
            Description = CurrentEducation.Description
        };

        if (IsEditing && _editingEducationId is not null)
        {
            ReplaceEducation(entry);
        }
        else
        {
            EducationEntries.Add(entry);
        }

        HasUnsavedChanges = true;

        await _localStorageService.SaveEducationDraftAsync(EducationEntries.ToList());
        if (!await SyncEducationAsync(entry))
        {
            ShowError("Education saved locally. Backend sync failed — please try again.");
        }
        else
        {
            await ShowToastAsync("Education saved");
        }
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
        if (entry is null || !EducationEntries.Contains(entry))
        {
            return;
        }

        if (!await ConfirmDeleteAsync("Delete education", "Delete this education entry?"))
        {
            return;
        }

        EducationEntries.Remove(entry);
        HasUnsavedChanges = true;
        await _localStorageService.SaveEducationDraftAsync(EducationEntries.ToList());
        if (!await _apiService.DeleteEducationAsync(entry.Id))
        {
            ShowError("Education removed locally. Backend delete failed — please try again.");
        }
        else
        {
            await ShowToastAsync("Education deleted");
        }
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

            await _localStorageService.SaveEducationDraftAsync(EducationEntries.ToList());

            var syncFailed = false;

            foreach (var entry in EducationEntries)
            {
                var success = int.TryParse(entry.Id, out _)
                    ? await _apiService.UpdateEducationAsync(entry)
                    : await _apiService.PostEducationAsync(entry);

                if (!success)
                {
                    syncFailed = true;
                    break;
                }
            }

            if (!syncFailed)
            {
                await _localStorageService.ClearEducationDraftAsync();
                HasUnsavedChanges = false;
            }

            if (syncFailed)
            {
                ShowError("Education saved locally. Backend sync failed — it will retry automatically.");
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
        var drafts = await _localStorageService.LoadEducationDraftAsync();
        if (drafts.Count > 0)
        {
            EducationEntries = new ObservableCollection<EducationEntry>(drafts);
            HasUnsavedChanges = true;
        }

        var entries = await _apiService.GetEducationAsync();
        if (entries.Count > 0)
        {
            EducationEntries = new ObservableCollection<EducationEntry>(entries);
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

    private async Task<bool> SyncEducationAsync(EducationEntry entry)
    {
        var success = int.TryParse(entry.Id, out _)
            ? await _apiService.UpdateEducationAsync(entry)
            : await _apiService.PostEducationAsync(entry);

        if (success)
        {
            await _localStorageService.SaveEducationDraftAsync(EducationEntries.ToList());
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

    partial void OnIsEditingChanged(bool value) => OnPropertyChanged(nameof(SubmitButtonText));

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanSave));

    partial void OnHasUnsavedChangesChanged(bool value) => OnPropertyChanged(nameof(CanSave));

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

    private void ReplaceEducation(EducationEntry entry)
    {
        var index = EducationEntries
            .Select((item, idx) => new { item, idx })
            .FirstOrDefault(x => x.item.Id == entry.Id)?.idx ?? -1;

        if (index >= 0)
        {
            EducationEntries[index] = entry;
        }
    }

    private void ResetEditor()
    {
        _editingEducationId = null;
        IsEditing = false;
        OnPropertyChanged(nameof(SubmitButtonText));
        CurrentEducation = new EducationEntry();
    }
}
