using Microsoft.Extensions.Configuration;
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
            fonts.AddFont("fa-brands-400.ttf", "FABrands");
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
}