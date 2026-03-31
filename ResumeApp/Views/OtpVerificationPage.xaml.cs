using ResumeApp.ViewModels;

namespace ResumeApp.Views;

public partial class OtpVerificationPage : ContentPage
{
    public OtpVerificationPage(OtpVerificationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
