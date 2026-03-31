using ResumeApp.ViewModels;

namespace ResumeApp.Views;

public partial class ExperiencePage : ContentPage
{
    public ExperiencePage(ExperienceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}