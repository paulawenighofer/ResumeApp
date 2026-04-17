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

            await Task.Delay(durationMilliseconds);
            popup.Close();
        });
}
