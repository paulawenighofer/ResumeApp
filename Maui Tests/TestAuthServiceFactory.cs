using Moq;
using ResumeApp.Services;

namespace MauiTests;

internal static class TestAuthServiceFactory
{
    public static AuthService Create()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler())
        {
            BaseAddress = new Uri("https://localhost")
        };

        var localStorage = new Mock<ILocalStorageService>();
        return new AuthService(httpClient, localStorage.Object);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));
        }
    }
}
