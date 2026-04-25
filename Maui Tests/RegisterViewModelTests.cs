using CommunityToolkit.Mvvm.Input;
using ResumeApp.ViewModels;

namespace MauiTests;

public class RegisterViewModelTests
{
    [Fact]
    public async Task RegisterCommand_WhenPasswordsDoNotMatch_SetsError()
    {
        var viewModel = new RegisterViewModel(TestAuthServiceFactory.Create())
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            Password = "Password123",
            ConfirmPassword = "Password124"
        };

        await ((IAsyncRelayCommand)viewModel.RegisterCommand).ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal("Please correct the highlighted fields.", viewModel.ErrorMessage);
        Assert.True(viewModel.HasConfirmPasswordValidation);
        Assert.Equal("Passwords don't match.", viewModel.ConfirmPasswordValidationMessage);
        Assert.False(viewModel.IsBusy);
    }
}
