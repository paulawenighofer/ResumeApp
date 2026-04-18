using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Models;
using ResumeApp.Services;
using ResumeApp.Views;
using Shared.Models;

namespace ResumeApp.ViewModels;

public partial class ResumeListViewModel : ObservableObject
{
    private readonly IApiService _apiService;

    [ObservableProperty]
    private bool hasResumes;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public ObservableCollection<ResumeDraftListItem> Drafts { get; } = [];

    public ResumeListViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    [RelayCommand]
    private async Task LoadDrafts()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var items = await _apiService.GetResumeDraftsAsync();

            Drafts.Clear();
            foreach (var item in items)
            {
                Drafts.Add(new ResumeDraftListItem
                {
                    Id = item.Id,
                    TargetCompany = item.TargetCompany,
                    Status = item.Status,
                    StatusText = item.Status switch
                    {
                        ResumeDraftStatus.Generated => "Ready",
                        ResumeDraftStatus.Failed => "Failed",
                        _ => "Generating"
                    },
                    StatusColorHex = item.Status switch
                    {
                        ResumeDraftStatus.Generated => "#16A34A",
                        ResumeDraftStatus.Failed => "#DC2626",
                        _ => "#7C3AED"
                    },
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    CreatedAtText = item.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy • hh:mm tt")
                });
            }

            HasResumes = Drafts.Count > 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Could not load drafts. {ex.Message}";
            HasResumes = Drafts.Count > 0;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenDraft(ResumeDraftListItem? item)
    {
        if (item is null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(ResumeDraftDetailPage)}?id={item.Id}");
    }

    [RelayCommand]
    private async Task GenerateResume() =>
        await Shell.Current.GoToAsync(nameof(GenerateResumePage));
}
