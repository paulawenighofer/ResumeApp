using CommunityToolkit.Mvvm.Input;
using ResumeApp.ViewModels;

namespace MauiTests;

public class LoginViewModelTests
{
    [Fact]
    public async Task LoginCommand_WhenCredentialsMissing_SetsError()
    {
        var viewModel = new LoginViewModel(TestAuthServiceFactory.Create())
        {
            Email = string.Empty,
            Password = string.Empty
        };

        await ((IAsyncRelayCommand)viewModel.LoginCommand).ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal("Please correct the highlighted fields.", viewModel.ErrorMessage);
        Assert.True(viewModel.HasEmailValidation);
        Assert.True(viewModel.HasPasswordValidation);
        Assert.False(viewModel.IsBusy);
    }
}
