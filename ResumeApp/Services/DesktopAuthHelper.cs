using System.Net;
using System.Text;

namespace ResumeApp.Services;

/// <summary>
/// Handles social login on Windows where WebAuthenticator isn't supported.
///
/// How it works:
/// 1. Starts a temporary HTTP server on a random available port
/// 2. Opens the browser to the backend's challenge URL, passing
///    the local server as the returnUrl
/// 3. User completes the OAuth flow in the browser
/// 4. Backend redirects to http://localhost:{port}?token=xxx
/// 5. Local server catches the redirect, extracts the token
/// 6. Sends a "You can close this tab" page to the browser
/// 7. Returns the token to the calling code
/// </summary>
public static class DesktopAuthHelper
{
    public static async Task<string?> AuthenticateAsync(string apiBaseUrl, string provider)
    {
        var listener = new HttpListener();
        var port = GetAvailablePort();
        var callbackUrl = $"http://localhost:{port}/";
        listener.Prefixes.Add(callbackUrl);
        listener.Start();

        try
        {
            // Open browser with the challenge URL
            // The returnUrl tells the backend where to redirect after OAuth completes
            var challengeUrl = $"{apiBaseUrl}/api/auth/{provider}-challenge"
                + $"?returnUrl={Uri.EscapeDataString(callbackUrl)}";

            await Browser.Default.OpenAsync(
                new Uri(challengeUrl), BrowserLaunchMode.SystemPreferred);

            // Wait for the browser to redirect back to our local listener
            // Timeout after 3 minutes in case the user abandons the flow
            var contextTask = listener.GetContextAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(3));
            var completedTask = await Task.WhenAny(contextTask, timeoutTask);

            if (completedTask == timeoutTask)
                return null; // Timed out

            var context = await contextTask;
            var query = context.Request.QueryString;
            var token = query["token"];

            // Send a response page to the browser
            var html = token != null
                ? BuildSuccessHtml()
                : BuildErrorHtml();

            var buffer = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.Close();

            return token;
        }
        finally
        {
            listener.Stop();
            listener.Close();
        }
    }

    /// <summary>
    /// Finds a random available TCP port. Uses the OS trick of binding
    /// to port 0, which assigns an available port, then immediately
    /// releasing it so our HttpListener can use it.
    /// </summary>
    private static int GetAvailablePort()
    {
        var tempListener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        tempListener.Start();
        var port = ((IPEndPoint)tempListener.LocalEndpoint).Port;
        tempListener.Stop();
        return port;
    }

    private static string BuildSuccessHtml()
    {
        return """
        <html>
        <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                     display: flex; justify-content: center; align-items: center;
                     height: 100vh; margin: 0; background: #F8F9FA;">
            <div style="text-align: center;">
                <div style="font-size: 48px; margin-bottom: 16px;">&#10003;</div>
                <h1 style="color: #1A1A2E; margin-bottom: 8px;">Signed in successfully!</h1>
                <p style="color: #6C757D;">You can close this tab and return to the app.</p>
            </div>
        </body>
        </html>
        """;
    }

    private static string BuildErrorHtml()
    {
        return """
        <html>
        <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                     display: flex; justify-content: center; align-items: center;
                     height: 100vh; margin: 0; background: #F8F9FA;">
            <div style="text-align: center;">
                <div style="font-size: 48px; margin-bottom: 16px;">&#10007;</div>
                <h1 style="color: #DC3545; margin-bottom: 8px;">Sign in failed</h1>
                <p style="color: #6C757D;">Please close this tab and try again in the app.</p>
            </div>
        </body>
        </html>
        """;
    }
}
