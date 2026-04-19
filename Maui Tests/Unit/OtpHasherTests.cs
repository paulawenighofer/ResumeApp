using API.Services;

namespace MauiTests.Unit;

public class OtpHasherTests
{
    [Fact]
    public void Hash_DoesNotReturnPlainCode()
    {
        var hash = OtpHasher.Hash("123456");
        Assert.NotEqual("123456", hash);
    }

    [Fact]
    public void Hash_ReturnsDifferentValueEachCall()
    {
        var hash1 = OtpHasher.Hash("123456");
        var hash2 = OtpHasher.Hash("123456");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectCode()
    {
        var code = "987654";
        var hash = OtpHasher.Hash(code);
        Assert.True(OtpHasher.Verify(code, hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongCode()
    {
        var hash = OtpHasher.Hash("111111");
        Assert.False(OtpHasher.Verify("222222", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForCorruptedHash_WithoutThrowing()
    {
        var result = OtpHasher.Verify("123456", "not-a-valid-bcrypt-hash");
        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_ForEmptyHash()
    {
        var result = OtpHasher.Verify("123456", "");
        Assert.False(result);
    }
}
