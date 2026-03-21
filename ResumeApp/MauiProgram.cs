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

        builder.Services.AddSingleton(sp =>
        {
            var client = new HttpClient();
#if ANDROID
            // Android emulator uses 10.0.2.2 to reach host machine's localhost
            client.BaseAddress = new Uri("https://10.0.2.2:7082/");
#else
    client.BaseAddress = new Uri("https://localhost:7082/");
#endif
            return client;
        });

        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<AppShell>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}