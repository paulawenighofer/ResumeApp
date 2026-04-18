using ResumeApp.ViewModels;

namespace ResumeApp.Views;

public partial class ResumeDraftDetailPage : ContentPage, IQueryAttributable
{
    public ResumeDraftDetailPage(ResumeDraftDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is IQueryAttributable queryAttributable)
        {
            queryAttributable.ApplyQueryAttributes(query);
        }
    }
}
