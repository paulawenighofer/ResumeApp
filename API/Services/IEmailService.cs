namespace API.Services
{
    public interface IEmailService
    {
        Task SendOtpAsync(string toEmail, string code);
        Task SendPasswordResetOtpAsync(string toEmail, string code);
    }
}
