namespace AuthService.Application.Services;

using System.Security.Cryptography;
using System.Text;

public sealed class EmailTokenService : IEmailTokenService
{
    private const int TokenSizeBytes = 48;

    public string GenerateToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenSizeBytes));

    public string HashToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(tokenBytes);

        return Convert.ToBase64String(hash);
    }

    public bool VerifyToken(string token, string hash)
    {
        byte[] expectedHash;

        try
        {
            expectedHash = Convert.FromBase64String(hash);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}