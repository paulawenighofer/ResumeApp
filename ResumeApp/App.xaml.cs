using ResumeApp.Services;

namespace ResumeApp;

public partial class App : Application
{
    private readonly AppShell _appShell;
    private readonly AuthService _authService;
    private readonly ISyncCoordinator _syncCoordinator;

    public App(AppShell appShell, AuthService authService, ISyncCoordinator syncCoordinator)
    {
        InitializeComponent();
        _appShell = appShell;
        _authService = authService;
        _syncCoordinator = syncCoordinator;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_appShell);
    }

    protected override async void OnStart()
    {
        base.OnStart();

        await _syncCoordinator.InitializeAsync();

        if (await _authService.IsLoggedInAsync())
        {
            await Shell.Current.GoToAsync("//main");
            await _syncCoordinator.SyncAllAsync();
        }
    }
}
