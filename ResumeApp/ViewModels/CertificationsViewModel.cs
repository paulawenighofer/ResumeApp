using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Models;
using ResumeApp.Services;
using System.Collections.ObjectModel;

namespace ResumeApp.ViewModels;

public partial class CertificationsViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly ILocalStorageService _localStorageService;

    [ObservableProperty]
    private CertificationEntry currentCertification = new();

    [ObservableProperty]
    private ObservableCollection<CertificationEntry> certificationEntries = [];

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isEditing;

    private string? _editingCertificationId;

    public string SubmitButtonText => IsEditing ? "Save changes" : "Add certification";

    public CertificationsViewModel(IApiService apiService, ILocalStorageService localStorageService)
    {
        _apiService = apiService;
        _localStorageService = localStorageService;
        _ = LoadDraftsAndEntriesAsync();
    }

    [RelayCommand]
    private async Task AddCertification()
    {
        ResetError();

        if (string.IsNullOrWhiteSpace(CurrentCertification.Name) ||
            string.IsNullOrWhiteSpace(CurrentCertification.IssuingOrganization))
        {
            ShowError("Please enter the certification name and issuing organization.");
            return;
        }

        if (CurrentCertification.ExpirationDate < CurrentCertification.IssueDate)
        {
            ShowError("Expiration date must be after the issue date.");
            return;
        }

        var entry = new CertificationEntry
        {
            Id = _editingCertificationId ?? Guid.NewGuid().ToString(),
            Name = CurrentCertification.Name.Trim(),
            IssuingOrganization = CurrentCertification.IssuingOrganization.Trim(),
            IssueDate = CurrentCertification.IssueDate,
            ExpirationDate = CurrentCertification.ExpirationDate,
            CredentialId = CurrentCertification.CredentialId,
            CredentialUrl = CurrentCertification.CredentialUrl
        };

        if (IsEditing && _editingCertificationId is not null)
        {
            ReplaceCertification(entry);
        }
        else
        {
            CertificationEntries.Add(entry);
        }

        await _localStorageService.SaveCertificationsDraftAsync(CertificationEntries.ToList());
        if (!await SyncCertificationAsync(entry))
        {
            ShowError("Certification saved locally. Backend sync failed — please try again.");
        }
        ResetEditor();
    }

    [RelayCommand]
    private void EditCertification(CertificationEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        _editingCertificationId = entry.Id;
        IsEditing = true;
        OnPropertyChanged(nameof(SubmitButtonText));
        CurrentCertification = new CertificationEntry
        {
            Id = entry.Id,
            Name = entry.Name,
            IssuingOrganization = entry.IssuingOrganization,
            IssueDate = entry.IssueDate,
            ExpirationDate = entry.ExpirationDate,
            CredentialId = entry.CredentialId,
            CredentialUrl = entry.CredentialUrl
        };
    }

    [RelayCommand]
    private void CancelEdit() => ResetEditor();

    [RelayCommand]
    private async Task DeleteCertification(CertificationEntry? entry)
    {
        if (entry is null || !CertificationEntries.Contains(entry))
        {
            return;
        }

        CertificationEntries.Remove(entry);
        await _localStorageService.SaveCertificationsDraftAsync(CertificationEntries.ToList());
        if (!await _apiService.DeleteCertificationAsync(entry.Id))
        {
            ShowError("Certification removed locally. Backend delete failed — please try again.");
        }
        if (_editingCertificationId == entry.Id)
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

            if (HasPendingCertificationInput())
            {
                var added = await TryAddCurrentCertificationAsDraftAsync();
                if (!added)
                {
                    return;
                }
            }

            if (CertificationEntries.Count == 0)
            {
                ShowError("Please add at least one certification.");
                return;
            }

            await _localStorageService.SaveCertificationsDraftAsync(CertificationEntries.ToList());

            var syncFailed = false;

            foreach (var certification in CertificationEntries)
            {
                var success = int.TryParse(certification.Id, out _)
                    ? await _apiService.UpdateCertificationAsync(certification)
                    : await _apiService.PostCertificationAsync(certification);

                if (!success)
                {
                    syncFailed = true;
                    break;
                }
            }

            if (!syncFailed)
            {
                await _localStorageService.ClearCertificationsDraftAsync();
            }

            if (syncFailed)
            {
                ShowError("Certifications saved locally. Backend sync failed — please try again.");
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
        var drafts = await _localStorageService.LoadCertificationsDraftAsync();
        if (drafts.Count > 0)
        {
            CertificationEntries = new ObservableCollection<CertificationEntry>(drafts);
        }

        var entries = await _apiService.GetCertificationsAsync();
        if (entries.Count > 0)
        {
            CertificationEntries = new ObservableCollection<CertificationEntry>(entries);
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

    private async Task<bool> SyncCertificationAsync(CertificationEntry entry)
    {
        var success = int.TryParse(entry.Id, out _)
            ? await _apiService.UpdateCertificationAsync(entry)
            : await _apiService.PostCertificationAsync(entry);

        if (success)
        {
            await _localStorageService.SaveCertificationsDraftAsync(CertificationEntries.ToList());
        }

        return success;
    }

    partial void OnIsEditingChanged(bool value) => OnPropertyChanged(nameof(SubmitButtonText));

    private bool HasPendingCertificationInput()
        => !string.IsNullOrWhiteSpace(CurrentCertification.Name)
        || !string.IsNullOrWhiteSpace(CurrentCertification.IssuingOrganization)
        || !string.IsNullOrWhiteSpace(CurrentCertification.CredentialId)
        || !string.IsNullOrWhiteSpace(CurrentCertification.CredentialUrl);

    private async Task<bool> TryAddCurrentCertificationAsDraftAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentCertification.Name) ||
            string.IsNullOrWhiteSpace(CurrentCertification.IssuingOrganization))
        {
            ShowError("Please complete the current certification form or clear it before continuing.");
            return false;
        }

        if (CurrentCertification.ExpirationDate < CurrentCertification.IssueDate)
        {
            ShowError("Expiration date must be after the issue date.");
            return false;
        }

        await AddCertification();
        return !HasError;
    }

    private void ReplaceCertification(CertificationEntry entry)
    {
        var index = CertificationEntries
            .Select((item, idx) => new { item, idx })
            .FirstOrDefault(x => x.item.Id == entry.Id)?.idx ?? -1;

        if (index >= 0)
        {
            CertificationEntries[index] = entry;
        }
    }

    private void ResetEditor()
    {
        _editingCertificationId = null;
        IsEditing = false;
        OnPropertyChanged(nameof(SubmitButtonText));
        CurrentCertification = new CertificationEntry();
    }
}
