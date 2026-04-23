namespace ResumeApp.Services;

public static class ToastService
{
    public static Task ShowAsync(string message, bool isError = false, int durationMilliseconds = 2000)
        => Task.CompletedTask;
}
