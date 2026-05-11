namespace AuthService.Application.Services;

using Shared.Constants;
using Shared.Results;

public sealed class PasswordValidator : IPasswordValidator
{
    public Result ValidatePassword(string password)
    {
        var unmetRequirements = GetUnmetRequirements(password);

        if (unmetRequirements.Count == 0)
        {
            return Result.Success();
        }

        return Result.Failure(Error.ValidationError(string.Join("; ", unmetRequirements)));
    }

    public List<string> GetUnmetRequirements(string password)
    {
        var requirements = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            requirements.Add("Password is required");
            return requirements;
        }

        if (password.Length < AuthConstants.MinimumPasswordLength)
        {
            requirements.Add($"Password must be at least {AuthConstants.MinimumPasswordLength} characters long");
        }

        if (!password.Any(char.IsUpper))
        {
            requirements.Add("Password must contain an uppercase letter");
        }

        if (!password.Any(char.IsLower))
        {
            requirements.Add("Password must contain a lowercase letter");
        }

        if (!password.Any(char.IsDigit))
        {
            requirements.Add("Password must contain a number");
        }

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            requirements.Add("Password must contain a special character");
        }

        return requirements;
    }
}