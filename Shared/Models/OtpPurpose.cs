namespace Shared.Models;

/// <summary>
/// Discriminator values stored in OtpVerification.Purpose.
/// Prevents a registration OTP from being used to reset a password, and vice versa.
/// </summary>
public static class OtpPurpose
{
    public const string EmailVerification = "EmailVerification";
    public const string PasswordReset = "PasswordReset";
}
