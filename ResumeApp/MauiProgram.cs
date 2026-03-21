using Microsoft.Extensions.Logging;
using ResumeApp.Services;
using ResumeApp.ViewModels;
using ResumeApp.Views;
using CommunityToolkit.Maui;

namespace ResumeApp;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        }).UseMauiCommunityToolkit();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<AppShell>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}