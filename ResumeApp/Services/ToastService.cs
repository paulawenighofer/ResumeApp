using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Shapes;
using System.Diagnostics;

namespace ResumeApp.Services;

public static class ToastService
{
    private const string ToastHostClassId = "ToastHostRoot";
    private const string ToastOverlayClassId = "ToastOverlayLayer";

    public static Task ShowAsync(string message, bool isError = false, int durationMilliseconds = 2000)
        => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var page = ResolveVisiblePage();
            var duration = Math.Max(durationMilliseconds, isError ? 5000 : 3000);

            try
            {
                if (page is ContentPage contentPage)
                {
                    await ShowCustomToastAsync(contentPage, message.Trim(), isError, duration);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Custom toast failed: {ex}");
            }

            try
            {
                if (page is not null)
                {
                    var options = new SnackbarOptions
                    {
                        BackgroundColor = isError ? Color.FromArgb("#B91C1C") : Color.FromArgb("#F4C542"),
                        TextColor = isError ? Colors.White : Color.FromArgb("#0F172A"),
                        ActionButtonTextColor = isError ? Colors.White : Color.FromArgb("#0F172A"),
                        CornerRadius = new CornerRadius(12)
                    };

                    await page.DisplaySnackbar(
                        (isError ? "⚠ " : "✓ ") + message,
                        action: () => { },
                        actionButtonText: "Close",
                        duration: TimeSpan.FromMilliseconds(duration),
                        visualOptions: options);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Snackbar failed: {ex}");
            }

            try
            {
                var toast = Toast.Make((isError ? "⚠ " : "✓ ") + message, isError ? ToastDuration.Long : ToastDuration.Short, 18);
                await toast.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Toast fallback failed: {ex}");
            }
        });

    private static async Task ShowCustomToastAsync(ContentPage page, string message, bool isError, int durationMilliseconds)
    {
        var overlay = GetOrCreateOverlay(page);
        overlay.InputTransparent = true;

        var backgroundColor = isError ? Color.FromArgb("#B91C1C") : Color.FromArgb("#F4C542");
        var textColor = isError ? Colors.White : Color.FromArgb("#0F172A");
        var iconGlyph = isError ? "\uf071" : "\uf058"; // triangle-exclamation / circle-check

        var closeIcon = new Label
        {
            Text = "\uf00d",
            FontFamily = "FASolid",
            FontSize = 18,
            TextColor = textColor,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            WidthRequest = 28,
            HeightRequest = 28,
            InputTransparent = true
        };

        var toastBorder = new Border
        {
            BackgroundColor = backgroundColor,
            StrokeThickness = 0,
            Padding = new Thickness(16, 14),
            Margin = new Thickness(0, 0, 0, 20),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            Shadow = new Shadow
            {
                Brush = Brush.Black,
                Opacity = 0.22f,
                Offset = new Point(0, 4),
                Radius = 12
            },
            Opacity = 0,
            TranslationY = 36,
            InputTransparent = true
        };

        var content = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12,
            MinimumHeightRequest = 52
        };

        content.Add(new Label
        {
            Text = iconGlyph,
            FontFamily = "FASolid",
            FontSize = 18,
            TextColor = textColor,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center
        }, 0, 0);

        content.Add(new Label
        {
            Text = message,
            TextColor = textColor,
            FontSize = 16,
            LineBreakMode = LineBreakMode.WordWrap,
            VerticalTextAlignment = TextAlignment.Center,
            MaxLines = 3
        }, 1, 0);

        content.Add(closeIcon, 2, 0);

        toastBorder.Content = content;

        overlay.Children.Clear();
        overlay.Children.Add(toastBorder);

        AbsoluteLayout.SetLayoutFlags(toastBorder, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional | Microsoft.Maui.Layouts.AbsoluteLayoutFlags.WidthProportional);
        AbsoluteLayout.SetLayoutBounds(toastBorder, new Rect(0.5, 1, 0.92, AbsoluteLayout.AutoSize));

        await Task.WhenAll(
            toastBorder.FadeTo(1, 180, Easing.CubicOut),
            toastBorder.TranslateTo(0, -12, 180, Easing.CubicOut));

        await Task.Delay(durationMilliseconds);

        await Task.WhenAll(
            toastBorder.FadeTo(0, 140, Easing.CubicIn),
            toastBorder.TranslateTo(0, 16, 140, Easing.CubicIn));

        overlay.Children.Remove(toastBorder);
        overlay.InputTransparent = true;
    }

    private static AbsoluteLayout GetOrCreateOverlay(ContentPage page)
    {
        if (page.Content is Grid hostGrid && hostGrid.ClassId == ToastHostClassId)
        {
            var existingOverlay = hostGrid.Children.OfType<AbsoluteLayout>().FirstOrDefault(x => x.ClassId == ToastOverlayClassId);
            if (existingOverlay is not null)
            {
                return existingOverlay;
            }

            var overlay = CreateOverlay();
            hostGrid.Children.Add(overlay);
            return overlay;
        }

        var originalContent = page.Content;
        var newHost = new Grid { ClassId = ToastHostClassId };

        if (originalContent is not null)
        {
            newHost.Children.Add(originalContent);
        }

        var newOverlay = CreateOverlay();
        newHost.Children.Add(newOverlay);
        page.Content = newHost;

        return newOverlay;
    }

    private static AbsoluteLayout CreateOverlay()
        => new()
        {
            ClassId = ToastOverlayClassId,
            InputTransparent = true,
            ZIndex = 9999
        };

    private static Page? ResolveVisiblePage()
    {
        var rootPage = Shell.Current ?? Application.Current?.Windows.FirstOrDefault()?.Page;
        return ResolvePageRecursive(rootPage);
    }

    private static Page? ResolvePageRecursive(Page? page)
        => page switch
        {
            Shell shell => ResolvePageRecursive(shell.CurrentPage),
            NavigationPage navigationPage => ResolvePageRecursive(navigationPage.CurrentPage),
            TabbedPage tabbedPage => ResolvePageRecursive(tabbedPage.CurrentPage),
            FlyoutPage flyoutPage => ResolvePageRecursive(flyoutPage.Detail),
            null => null,
            _ => page
        };
}
