namespace AuthService.Application.Services;

using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Shared.Constants;

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));
        argon2.Salt = salt;
        argon2.DegreeOfParallelism = AuthConstants.PasswordHashParallelism;
        argon2.Iterations = AuthConstants.PasswordHashIterations;
        argon2.MemorySize = AuthConstants.PasswordHashMemorySize;

        var hash = argon2.GetBytes(HashSize);

        return string.Join('$',
            "argon2id",
            AuthConstants.PasswordHashIterations,
            AuthConstants.PasswordHashMemorySize,
            AuthConstants.PasswordHashParallelism,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool VerifyPassword(string password, string hash)
    {
        var parts = hash.Split('$', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 6 || !string.Equals(parts[0], "argon2id", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) ||
            !int.TryParse(parts[2], out var memorySize) ||
            !int.TryParse(parts[3], out var degreeOfParallelism))
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;

        try
        {
            salt = Convert.FromBase64String(parts[4]);
            expectedHash = Convert.FromBase64String(parts[5]);
        }
        catch (FormatException)
        {
            return false;
        }

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));
        argon2.Salt = salt;
        argon2.DegreeOfParallelism = degreeOfParallelism;
        argon2.Iterations = iterations;
        argon2.MemorySize = memorySize;

        var actualHash = argon2.GetBytes(expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}