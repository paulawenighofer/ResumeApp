using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Views;
using ResumeApp.Views.Controls;

namespace ResumeApp.ViewModels;

public partial class BottomNavBarViewModel : ObservableObject
{
    [ObservableProperty]
    private NavTab selectedTab = NavTab.Home;

    [RelayCommand]
    private async Task GoToHome()
    {
        SelectedTab = NavTab.Home;
        await Shell.Current.GoToAsync("//main/home");
    }

    [RelayCommand]
    private async Task GoToResume()
    {
        SelectedTab = NavTab.Resume;
        await Shell.Current.GoToAsync("//main/resume");
    }

    [RelayCommand]
    private async Task GoToProfile()
    {
        SelectedTab = NavTab.Profile;
        await Shell.Current.GoToAsync("//main/profile");
    }

    [RelayCommand]
    private async Task GoToSettings()
    {
        SelectedTab = NavTab.Settings;
        await Shell.Current.GoToAsync("//main/settings");
    }

    [RelayCommand]
    private async Task OpenGenerate() =>
        await Shell.Current.GoToAsync(nameof(GenerateResumePage));
}
