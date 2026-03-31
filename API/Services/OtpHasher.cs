namespace API.Services;

/// <summary>
/// Hashes and verifies OTP codes using BCrypt.
///
/// BCrypt is intentionally slow (work factor 10 ≈ 100ms per hash).
/// Even though a 6-digit code has only 1,000,000 possible values,
/// an attacker with the database would need ~100,000 seconds (over a day)
/// to brute-force a single code offline — well beyond the 10-minute expiry.
/// BCrypt also generates a unique salt per hash automatically.
/// </summary>
public static class OtpHasher
{
    private const int WorkFactor = 10;

    public static string Hash(string code) =>
        BCrypt.Net.BCrypt.HashPassword(code, WorkFactor);

    public static bool Verify(string code, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(code, hash);
        }
        catch
        {
            // hash is not a valid BCrypt string (e.g., a legacy plain-text or HMAC OTP).
            // Treat it as a mismatch — never throw a 500 back to the client.
            return false;
        }
    }
}
