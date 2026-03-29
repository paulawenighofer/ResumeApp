using ResumeApp.ViewModels;

namespace ResumeApp.Views;

public partial class SkillsPage : ContentPage
{
    public SkillsPage(SkillsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}