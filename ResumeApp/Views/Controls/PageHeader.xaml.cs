using System.Windows.Input;

namespace ResumeApp.Views.Controls;

public partial class PageHeader : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title),
        typeof(string),
        typeof(PageHeader),
        string.Empty);

    public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(
        nameof(ActionText),
        typeof(string),
        typeof(PageHeader),
        string.Empty);

    public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(
        nameof(ActionCommand),
        typeof(ICommand),
        typeof(PageHeader));

    public static readonly BindableProperty ActionEnabledProperty = BindableProperty.Create(
        nameof(ActionEnabled),
        typeof(bool),
        typeof(PageHeader),
        true);

    public static readonly BindableProperty IsActionVisibleProperty = BindableProperty.Create(
        nameof(IsActionVisible),
        typeof(bool),
        typeof(PageHeader),
        false);

    public PageHeader()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public bool ActionEnabled
    {
        get => (bool)GetValue(ActionEnabledProperty);
        set => SetValue(ActionEnabledProperty, value);
    }

    public bool IsActionVisible
    {
        get => (bool)GetValue(IsActionVisibleProperty);
        set => SetValue(IsActionVisibleProperty, value);
    }

    private async void OnBackTapped(object sender, TappedEventArgs e)
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
