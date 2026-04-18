using ResumeApp.ViewModels;

namespace ResumeApp.Views;

public partial class ResumeListPage : ContentPage
{
    public ResumeListPage(ResumeListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
