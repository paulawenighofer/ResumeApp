using ResumeApp.ViewModels;

namespace ResumeApp.Views;

public partial class ProjectsPage : ContentPage
{
    public ProjectsPage(ProjectsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
