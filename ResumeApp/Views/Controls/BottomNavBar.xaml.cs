using ResumeApp.ViewModels;

namespace ResumeApp.Views.Controls;

public enum NavTab { Home, Resume, Profile, Settings }

public partial class BottomNavBar : ContentView
{
    public BottomNavBar()
    {
        InitializeComponent();
        BindingContext = IPlatformApplication.Current?.Services.GetRequiredService<BottomNavBarViewModel>();
    }
}
