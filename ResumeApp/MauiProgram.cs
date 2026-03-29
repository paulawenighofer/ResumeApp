using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using ResumeApp.Services;
using ResumeApp.ViewModels;
using ResumeApp.Views;

namespace ResumeApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        ConfigureServices(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(DeviceInfo.Platform == DevicePlatform.Android
                ? "https://10.0.2.2:7082/"
                : "https://localhost:7082/")
        });

        services.AddSingleton<AuthService>();
        services.AddSingleton<IApiService, ApiService>();
        services.AddSingleton<ILocalStorageService, LocalStorageService>();

        services.AddSingleton<AppShell>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<EducationViewModel>();
        services.AddTransient<ExperienceViewModel>();
        services.AddTransient<SkillsViewModel>();
        services.AddTransient<ProjectsViewModel>();

        services.AddTransient<LoginPage>();
        services.AddTransient<RegisterPage>();
        services.AddTransient<MainPage>();
        services.AddTransient<EducationPage>();
        services.AddTransient<ExperiencePage>();
        services.AddTransient<SkillsPage>();
        services.AddTransient<ProjectsPage>();
    }
}
