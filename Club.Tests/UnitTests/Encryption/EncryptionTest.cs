using Microsoft.Extensions.Options;
using Club.Common.Config;
using Club.Services;

namespace UnitTests.Encryption;

public class EncryptionTest
{
    private readonly EncryptionService _encryptionService;
    public EncryptionTest()
    {
        var options = Options.Create(new AppConfig
        {
            EncryptionKey = "Your16CharKeyHere",
            EncryptionSalt = "Your16CharSaltHere",
        });
        _encryptionService = new EncryptionService(options);
    }

    [Theory]
    [InlineData("test")]
    [InlineData("longer string with spaces")]
    [InlineData("&%$#!$%$!@#")]
    [InlineData("00000000000")]
    public void Encrypt_Test(string plainText)
    {
        var iv = _encryptionService.GenerateIV();
        Assert.NotNull(iv);

        var encryptedValue = _encryptionService.Encrypt(plainText, iv);
        var decryptedValue = _encryptionService.Decrypt(encryptedValue, iv);
        Assert.Equivalent(decryptedValue, plainText);
    }

    [Fact]
    public void Encrypt_To()
    {
        string plainText = "test";
        // Create static byte array for IV generation
        var iv = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        // var iv = encryptionService.GenerateIV();
        Assert.NotNull(iv);

        var encryptedValue = _encryptionService.Encrypt(plainText, iv);
        // var decryptedValue = encryptionService.Decrypt(encryptedValue, iv);
        Assert.Equivalent(encryptedValue, "/DFnCeCOB+2RHR+qVp2Z3A==");

        var decryptedValue = _encryptionService.Decrypt(encryptedValue, iv);
        Assert.Equivalent(decryptedValue, plainText);
    }

    [Fact]
    public void Encrypt_Long()
    {
        string plainText = "KzI3ODQyNTAyMzExLmVhYzNhNTg3LTQ4NTUtNGY5YS1hNmMxLTNkMDA1NjM3OWVlYg==";
        // Create static byte array for IV generation
        var iv = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        // var iv = encryptionService.GenerateIV();
        Assert.NotNull(iv);

        var encryptedValue = _encryptionService.Encrypt(plainText, iv);
        // var decryptedValue = encryptionService.Decrypt(encryptedValue, iv);
        Assert.Equivalent(encryptedValue, "PMIixucl3e5WDJZzhlofrgexKgl8kueNdI1gUV0hi/VeiO6fDhr8t5krKOT6ZYoxTGJY2rgADO1FE/KKhO2TZwOVsiUn+z2XS/Ux7WOmJj8=");

        var decryptedValue = _encryptionService.Decrypt(encryptedValue, iv);
        Assert.Equivalent(decryptedValue, plainText);
    }
}
