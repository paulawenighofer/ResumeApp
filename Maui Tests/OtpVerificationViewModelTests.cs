using CommunityToolkit.Mvvm.Input;
using ResumeApp.ViewModels;

namespace MauiTests;

public class OtpVerificationViewModelTests
{
    [Fact]
    public async Task VerifyCommand_WhenCodeIsInvalid_SetsError()
    {
        var viewModel = new OtpVerificationViewModel(TestAuthServiceFactory.Create())
        {
            Email = "jane@example.com",
            Code = "123"
        };

        await ((IAsyncRelayCommand)viewModel.VerifyCommand).ExecuteAsync(null);

        Assert.True(viewModel.HasError);
        Assert.Equal("Please enter the 6-digit code from your email.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }
}
