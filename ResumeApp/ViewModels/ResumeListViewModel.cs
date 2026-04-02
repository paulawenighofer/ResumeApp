using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Views;

namespace ResumeApp.ViewModels;

public partial class ResumeListViewModel : ObservableObject
{
    [ObservableProperty]
    private bool hasResumes = false;

    [RelayCommand]
    private async Task GenerateResume() =>
        await Shell.Current.GoToAsync(nameof(GenerateResumePage));
}
