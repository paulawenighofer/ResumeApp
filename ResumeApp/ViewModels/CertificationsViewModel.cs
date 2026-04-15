using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Models;
using ResumeApp.Services;
using System.Collections.ObjectModel;

namespace ResumeApp.ViewModels;

public partial class CertificationsViewModel : ObservableObject
{
    private readonly ILocalStorageService _localStorageService;
    private readonly ISyncCoordinator _syncCoordinator;

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

    public CertificationsViewModel(ILocalStorageService localStorageService, ISyncCoordinator syncCoordinator)
    {
        _localStorageService = localStorageService;
        _syncCoordinator = syncCoordinator;
        _ = LoadEntriesAsync();
    }

    [RelayCommand]
    private async Task AddCertification()
    {
        ResetError();
        if (string.IsNullOrWhiteSpace(CurrentCertification.Name) || string.IsNullOrWhiteSpace(CurrentCertification.IssuingOrganization))
        {
            ShowError("Please provide the certification name and issuer.");
            return;
        }

        var existing = certificationEntries.FirstOrDefault(x => x.Id == _editingCertificationId);
        var entry = new CertificationEntry
        {
            Id = existing?.Id ?? Guid.NewGuid().ToString(),
            UpdatedAt = existing?.UpdatedAt,
            Version = existing?.Version,
            Deleted = false,
            Name = CurrentCertification.Name.Trim(),
            IssuingOrganization = CurrentCertification.IssuingOrganization.Trim(),
            IssueDate = CurrentCertification.IssueDate,
            ExpirationDate = CurrentCertification.ExpirationDate,
            CredentialId = CurrentCertification.CredentialId,
            CredentialUrl = CurrentCertification.CredentialUrl
        };

        await _localStorageService.SaveItemAsync(entry);
        await RefreshEntriesAsync();
        _ = _syncCoordinator.SyncCertificationsAsync();
        ResetEditor();
    }

    [RelayCommand]
    private void EditCertification(CertificationEntry? entry)
    {
        if (entry is null) return;
        _editingCertificationId = entry.Id;
        IsEditing = true;
        CurrentCertification = new CertificationEntry
        {
            Id = entry.Id,
            UpdatedAt = entry.UpdatedAt,
            Version = entry.Version,
            Deleted = entry.Deleted,
            Name = entry.Name,
            IssuingOrganization = entry.IssuingOrganization,
            IssueDate = entry.IssueDate,
            ExpirationDate = entry.ExpirationDate,
            CredentialId = entry.CredentialId,
            CredentialUrl = entry.CredentialUrl
        };
    }

    [RelayCommand]
    private async Task DeleteCertification(CertificationEntry? entry)
    {
        if (entry is null) return;
        await _localStorageService.DeleteItemAsync(entry);
        await RefreshEntriesAsync();
        _ = _syncCoordinator.SyncCertificationsAsync();
        if (_editingCertificationId == entry.Id) ResetEditor();
    }

    [RelayCommand]
    private void CancelEdit() => ResetEditor();

    [RelayCommand]
    private async Task SaveAndContinue()
    {
        if (!string.IsNullOrWhiteSpace(CurrentCertification.Name) || !string.IsNullOrWhiteSpace(CurrentCertification.IssuingOrganization))
        {
            await AddCertification();
        }

        await _syncCoordinator.SyncCertificationsAsync();
        await Shell.Current.GoToAsync("..");
    }

    private async Task LoadEntriesAsync()
    {
        await _localStorageService.InitializeAsync();
        await RefreshEntriesAsync();
        _ = Task.Run(async () =>
        {
            await _syncCoordinator.SyncCertificationsAsync();
            await MainThread.InvokeOnMainThreadAsync(RefreshEntriesAsync);
        });
    }

    private async Task RefreshEntriesAsync()
        => CertificationEntries = new ObservableCollection<CertificationEntry>(await _localStorageService.LoadItemsAsync<CertificationEntry>());

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

    private void ResetEditor()
    {
        _editingCertificationId = null;
        IsEditing = false;
        CurrentCertification = new CertificationEntry();
    }
}
