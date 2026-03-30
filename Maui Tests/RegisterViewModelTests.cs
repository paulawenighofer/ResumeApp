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
        Assert.Equal("Passwords don't match.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }
}
