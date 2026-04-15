using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Models;
using ResumeApp.Services;
using ResumeApp.Views;
using System.Collections.ObjectModel;

namespace ResumeApp.ViewModels;

public partial class ResumeListViewModel : ObservableObject
{
    private readonly ILocalStorageService _localStorageService;
    private readonly ISyncCoordinator _syncCoordinator;

    [ObservableProperty]
    private ObservableCollection<StoredResumeEntry> resumes = [];

    [ObservableProperty]
    private bool hasResumes;

    public ResumeListViewModel(ILocalStorageService localStorageService, ISyncCoordinator syncCoordinator)
    {
        _localStorageService = localStorageService;
        _syncCoordinator = syncCoordinator;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task GenerateResume() =>
        await Shell.Current.GoToAsync(nameof(GenerateResumePage));

    [RelayCommand]
    private async Task DeleteResume(StoredResumeEntry? resume)
    {
        if (resume is null)
        {
            return;
        }

        await _localStorageService.DeleteItemAsync(resume);
        await RefreshAsync();
        _ = _syncCoordinator.SyncResumesAsync();
    }

    private async Task LoadAsync()
    {
        await _localStorageService.InitializeAsync();
        await RefreshAsync();
        _ = Task.Run(async () =>
        {
            await _syncCoordinator.SyncResumesAsync();
            await MainThread.InvokeOnMainThreadAsync(RefreshAsync);
        });
    }

    private async Task RefreshAsync()
    {
        Resumes = new ObservableCollection<StoredResumeEntry>(await _localStorageService.LoadItemsAsync<StoredResumeEntry>());
        HasResumes = Resumes.Count > 0;
    }
}
