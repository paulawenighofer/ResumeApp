using ResumeApp.ViewModels;

namespace ResumeApp.Views;

public partial class GenerateResumePage : ContentPage
{
    public GenerateResumePage(GenerateResumeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
