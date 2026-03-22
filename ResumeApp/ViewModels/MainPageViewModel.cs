using CommunityToolkit.Mvvm.ComponentModel;

namespace ResumeApp.ViewModels;

public class MainPageViewModel : ObservableObject
{
    private string _userName = "User";
    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    public string AppHeading { get; } = "AI Resume Builder";

    public MainPageViewModel()
    {
        _ = LoadUserNameAsync();
    }

    private async Task LoadUserNameAsync()
    {
        var savedName = await SecureStorage.GetAsync("user_name");

        if (!string.IsNullOrWhiteSpace(savedName))
            UserName = savedName;
    }
}
