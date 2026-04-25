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
        Assert.Equal("Please correct the highlighted fields.", viewModel.ErrorMessage);
        Assert.True(viewModel.HasCodeValidation);
        Assert.Equal("Reset code must be 6 digits.", viewModel.CodeValidationMessage);
        Assert.False(viewModel.IsBusy);
    }
}
