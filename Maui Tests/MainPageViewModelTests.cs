using ResumeApp.ViewModels;

namespace MauiTests;

public class MainPageViewModelTests
{
    [Fact]
    public void Constructor_SetsExpectedDefaults()
    {
        var viewModel = new MainPageViewModel(TestAuthServiceFactory.Create());

        Assert.Equal("AI Resume Builder", viewModel.AppHeading);
        Assert.Equal("User", viewModel.UserName);
        Assert.Equal(string.Empty, viewModel.UserEmail);
    }
}
