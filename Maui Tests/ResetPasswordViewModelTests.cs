using CommunityToolkit.Mvvm.Input;
using ResumeApp.ViewModels;

namespace MauiTests;

public class ResetPasswordViewModelTests
{
    [Fact]
    public async Task ResetPasswordCommand_WhenCodeIsInvalid_SetsError()
    {
        var viewModel = new ResetPasswordViewModel(TestAuthServiceFactory.Create())
        {
            Email = "jane@example.com",
            Code = "123",
            NewPassword = "Password123",
            ConfirmPassword = "Password123"
        };

        await ((IAsyncRelayCommand)viewModel.ResetPasswordCommand).ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal("Please enter the 6-digit reset code.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }
}
