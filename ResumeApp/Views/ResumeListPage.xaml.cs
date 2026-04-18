using ResumeApp.ViewModels;

namespace ResumeApp.Views;

public partial class ResumeListPage : ContentPage
{
    private readonly ResumeListViewModel _viewModel;

    public ResumeListPage(ResumeListViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDraftsCommand.ExecuteAsync(null);
    }
}
