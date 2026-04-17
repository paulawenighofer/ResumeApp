using CommunityToolkit.Maui.Views;

namespace ResumeApp.Views.Controls;

public partial class ToastPopup : Popup
{
    public ToastPopup(string message, bool isError)
    {
        InitializeComponent();
        Message = message;
        BackgroundColor = isError ? Color.FromArgb("#FEE2E2") : Color.FromArgb("#EDE9FE");
        TextColor = isError ? Color.FromArgb("#B91C1C") : Color.FromArgb("#4C1D95");
        BindingContext = this;
    }

    public VisualElement ToastContainerView => ToastContainer;

    public string Message { get; }

    public Color BackgroundColor { get; }

    public Color TextColor { get; }
}
