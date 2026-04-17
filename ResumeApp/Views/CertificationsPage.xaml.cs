using ResumeApp.ViewModels;

namespace ResumeApp.Views;

public partial class CertificationsPage : ContentPage
{
    public CertificationsPage(CertificationsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
