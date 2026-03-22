using ResumeApp.Services;

namespace ResumeApp;

public partial class App : Application
{
    private readonly AppShell _appShell;
    private readonly AuthService _authService;

    public App(AppShell appShell, AuthService authService)
    {
        InitializeComponent();
        _appShell = appShell;
        _authService = authService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_appShell);
    }

    protected override async void OnStart()
    {
        base.OnStart();

        if (await _authService.IsLoggedInAsync())
            await Shell.Current.GoToAsync("//main");
    }
}
