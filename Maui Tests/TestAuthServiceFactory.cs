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

        return new AuthService(httpClient);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));
        }
    }
}
