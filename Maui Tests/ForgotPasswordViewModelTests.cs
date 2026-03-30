using CommunityToolkit.Mvvm.Input;
using ResumeApp.ViewModels;

namespace MauiTests;

public class ForgotPasswordViewModelTests
{
    [Fact]
    public async Task SendResetLinkCommand_WhenEmailMissing_SetsError()
    {
        var viewModel = new ForgotPasswordViewModel(TestAuthServiceFactory.Create())
        {
            Email = string.Empty
        };

        await ((IAsyncRelayCommand)viewModel.SendResetLinkCommand).ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal("Please enter your email address.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }
}
