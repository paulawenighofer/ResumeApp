using ResumeApp.ViewModels;

namespace ResumeApp.Views;

public partial class EducationPage : ContentPage
{
    public EducationPage(EducationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}