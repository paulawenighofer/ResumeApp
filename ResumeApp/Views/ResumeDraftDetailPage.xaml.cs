using ResumeApp.ViewModels;

namespace ResumeApp.Views;

public partial class ResumeDraftDetailPage : ContentPage, IQueryAttributable
{
    private readonly ResumeDraftDetailViewModel _viewModel;
    private int _draftId;

    public ResumeDraftDetailPage(ResumeDraftDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var raw) &&
            int.TryParse(raw?.ToString(), out var id))
        {
            _draftId = id;
            await _viewModel.LoadDraftAsync(id);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_draftId > 0)
        {
            await _viewModel.LoadDraftAsync(_draftId);
        }
    }
}
