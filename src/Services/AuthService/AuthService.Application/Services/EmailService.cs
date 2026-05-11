namespace AuthService.Application.Services;

using AuthService.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Results;

public sealed class EmailService : IEmailService
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly ILogger<EmailService> _logger;
    private readonly bool _autoVerifyEmail;

    public EmailService(
        IAuthUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _autoVerifyEmail = bool.TryParse(configuration["AuthDemo:AutoVerifyEmail"], out var enabled) && enabled;
    }

    public Task<Result> SendEmailVerificationAsync(string email, string verificationLink, CancellationToken cancellationToken = default)
    {
        return SendEmailVerificationInternalAsync(email, cancellationToken);
    }

    public Task<Result> SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SendLoginAlertAsync(string email, string ipAddress, string userAgent, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }

    private async Task<Result> SendEmailVerificationInternalAsync(string email, CancellationToken cancellationToken)
    {
        if (!_autoVerifyEmail)
        {
            _logger.LogInformation("Demo auto-verification is disabled. Verification email simulated for {Email}.", email);
            return Result.Success();
        }

        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Demo auto-verification skipped. User with email {Email} was not found.", email);
            return Result.Success();
        }

        if (!user.EmailVerified)
        {
            user.VerifyEmail();
            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
        }

        var pendingToken = await _unitOfWork.EmailVerificationTokens
            .GetPendingTokenAsync(user.Id, email, cancellationToken);

        if (pendingToken is not null && pendingToken.IsValid)
        {
            pendingToken.MarkAsVerified();
            await _unitOfWork.EmailVerificationTokens.UpdateAsync(pendingToken, cancellationToken);
        }

        _logger.LogInformation("Demo auto-verification completed for {Email}.", email);
        return Result.Success();
    }
}