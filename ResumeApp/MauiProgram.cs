using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
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

        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        }).UseMauiCommunityToolkit();

        builder.Services.AddSingleton(sp =>
        {
#if DEBUG
            // In development, point to your local API server.
            // Change this to match wherever your API is running (e.g. https://localhost:7082).
            // The production URL in appsettings.json is used for release builds automatically.
            var apiBaseUrl = "https://localhost:7082";
#else
            var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
                ?? throw new InvalidOperationException("ApiBaseUrl is not set in appsettings.json.");
#endif
            var client = new HttpClient();
            client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + '/');
            return client;
        });

        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<OtpVerificationViewModel>();
        builder.Services.AddTransient<OtpVerificationPage>();
        builder.Services.AddTransient<ForgotPasswordViewModel>();
        builder.Services.AddTransient<ForgotPasswordPage>();
        builder.Services.AddTransient<ResetPasswordViewModel>();
        builder.Services.AddTransient<ResetPasswordPage>();
        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<AppShell>();
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
