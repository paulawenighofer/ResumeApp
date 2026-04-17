using CommunityToolkit.Maui.Views;
using ResumeApp.Views.Controls;

namespace ResumeApp.Services;

public static class ToastService
{
    public static Task ShowAsync(string message, bool isError = false, int durationMilliseconds = 2000)
        => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Application.Current?.MainPage is null)
            {
                return;
            }

            var popup = new ToastPopup(message, isError);
            Application.Current.MainPage.ShowPopup(popup);

            var container = popup.ToastContainerView;
            container.Opacity = 0;
            container.TranslationY = 20;

            await Task.WhenAll(
                container.FadeTo(1, 180, Easing.CubicOut),
                container.TranslateTo(0, 0, 180, Easing.CubicOut));

            await Task.Delay(durationMilliseconds);

            await Task.WhenAll(
                container.FadeTo(0, 180, Easing.CubicIn),
                container.TranslateTo(0, 10, 180, Easing.CubicIn));

            popup.Close();
        });
}
