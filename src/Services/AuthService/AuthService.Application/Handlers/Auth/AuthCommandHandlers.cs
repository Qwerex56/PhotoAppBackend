using System.Data.Common;
using AuthService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MassTransit;
using Shared.Contracts.Events.Auth;

namespace AuthService.Application.Handlers.Auth;

using MediatR;
using Shared.Results;
using Shared.Constants;
using Domain.Repositories;
using Services;
using Commands.Auth;
using System.Security.Claims;
using System.Data;
using System.Security.Principal;

/// <summary>
/// Handles user registration command.
/// Creates a new user account and initiates email verification process.
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterCommandResponse>>
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailTokenService _emailTokenService;
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IConfiguration _configuration;

    public RegisterCommandHandler(
        IAuthUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEmailTokenService emailTokenService,
        IEmailService emailService,
        IUserRepository userRepository,
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IPublishEndpoint publishEndpoint,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _emailTokenService = emailTokenService;
        _emailService = emailService;
        _userRepository = userRepository;
        _emailVerificationTokenRepository = emailVerificationTokenRepository;
        _publishEndpoint = publishEndpoint;
        _configuration = configuration;
    }

    public async Task<Result<RegisterCommandResponse>> Handle(RegisterCommand request,
        CancellationToken cancellationToken)
    {
        // Check if user exists
        var userExists = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);

        if (userExists)
        {
            return Result<RegisterCommandResponse>.Failure(Error.Conflict(ErrorCodes.UserAlreadyExists));
        }

        // Create new user and email verification token
        // TODO: Missing full name in request
        var hashedPassword = _passwordHasher.HashPassword(request.Password);
        var user = new User(Guid.NewGuid(), request.Email, hashedPassword);

        var verificationToken = _emailTokenService.GenerateToken();
        var emailToken = new EmailVerificationToken(user.Id,
            user.Email,
            _emailTokenService.HashToken(verificationToken),
            DateTime.UtcNow.AddMinutes(AuthConstants.EmailVerificationTokenExpirationMinutes),
            request.IpAddress);

        await _userRepository.CreateAsync(user, cancellationToken);
        await _emailVerificationTokenRepository.CreateAsync(emailToken, cancellationToken);
        
        // Save to database
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbException e)
        {
            return Result<RegisterCommandResponse>.Failure(Error.Conflict(e.Message));
        }
        
        var verificationLink = BuildVerificationLink(user.Id, user.Email, verificationToken);
        var emailResult = await _emailService.SendEmailVerificationAsync(user.Email, verificationLink, cancellationToken);

        if (emailResult.IsFailure)
        {
            return Result<RegisterCommandResponse>.Failure(emailResult.Error!);
        }

        await _publishEndpoint.Publish(new UserRegisteredEvent
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.Email,
            RegisteredAt = DateTime.UtcNow,
            Source = "AuthService.Application"
        }, cancellationToken);

        // Return operation result
        return Result<RegisterCommandResponse>.Success(new RegisterCommandResponse
        {
            UserId = user.Id,
            Email = user.Email,
        });
    }

    private string BuildVerificationLink(Guid userId, string email, string token)
    {
        var baseUrl = _configuration["AuthEmail:VerificationLinkBaseUrl"]
            ?? "https://auth.localhost/api/auth/verify-email";
        var separator = baseUrl.Contains('?') ? "&" : "?";

        return $"{baseUrl}{separator}userId={Uri.EscapeDataString(userId.ToString())}&email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }
}

/// <summary>
/// Handles user login command.
/// Validates credentials and returns JWT access token + refresh token.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginCommandResponse>>
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailTokenService _emailTokenService;
    private readonly IEmailService _emailService;

    public LoginCommandHandler(
        IAuthUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEmailTokenService emailTokenService,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailTokenService = emailTokenService;
        _emailService = emailService;
    }

    public async Task<Result<LoginCommandResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            return Result<LoginCommandResponse>.Failure(Error.Unauthorized(ErrorCodes.InvalidCredentials));
        }

        if (user.IsLocked)
        {
            return Result<LoginCommandResponse>.Failure(Error.Forbidden("Account is locked"));
        }

        if (!user.EmailVerified)
        {
            return Result<LoginCommandResponse>.Failure(Error.Forbidden(ErrorCodes.EmailNotVerified));
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.RecordFailedLoginAttempt();
            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

            return Result<LoginCommandResponse>.Failure(Error.Unauthorized(ErrorCodes.InvalidCredentials));
        }

        var mfaCode = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var challengeToken = _tokenService.GenerateChallengeToken(new Dictionary<string, string>
        {
            ["purpose"] = "login_email_mfa",
            ["userId"] = user.Id.ToString(),
            ["email"] = user.Email,
            ["codeHash"] = _emailTokenService.HashToken(mfaCode)
        }, TimeSpan.FromMinutes(AuthConstants.MfaChallengeExpirationMinutes));

        Console.WriteLine("DELETE AFTER TESTING");
        Console.WriteLine($"Code: {mfaCode}, code hash: {_emailTokenService.HashToken(mfaCode)}, email: {user.Email}");
        Console.WriteLine("DELETE AFTER TESTING");

        var emailResult = await _emailService.SendTwoFactorCodeAsync(user.Email, mfaCode, cancellationToken);

        if (emailResult.IsFailure)
        {
            return Result<LoginCommandResponse>.Failure(emailResult.Error!);
        }

        return Result<LoginCommandResponse>.Success(new LoginCommandResponse
        {
            UserId = user.Id,
            Email = user.Email,
            RequiresMfa = true,
            MfaChallenge = challengeToken,
            MfaMethod = "email"
        });
    }
}

public class CompleteEmailMfaCommandHandler : IRequestHandler<CompleteEmailMfaCommand, Result<LoginCommandResponse>>
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IEmailTokenService _emailTokenService;
    private readonly IPublishEndpoint _publishEndpoint;

    public CompleteEmailMfaCommandHandler(
        IAuthUnitOfWork unitOfWork,
        ITokenService tokenService,
        IEmailTokenService emailTokenService,
        IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _emailTokenService = emailTokenService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<LoginCommandResponse>> Handle(CompleteEmailMfaCommand request, CancellationToken cancellationToken)
    {
        var claims = _tokenService.ValidateAndExtractClaims(request.ChallengeToken);
        if (claims is null)
        {
            return Result<LoginCommandResponse>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        foreach (var key in claims.Keys)
        {
            Console.WriteLine($"{key}: {claims[key]}");
        }

        if (!claims.TryGetValue("purpose", out var purpose))
        {
            Console.WriteLine("DELETE AFTER TESTING");
            Console.WriteLine($"purpose test");
            Console.WriteLine("DELETE AFTER TESTING");
            return Result<LoginCommandResponse>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        if (!string.Equals(purpose, "login_email_mfa", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("DELETE AFTER TESTING");
            Console.WriteLine($"purpose compare test");
            Console.WriteLine("DELETE AFTER TESTING");
            return Result<LoginCommandResponse>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        if (!claims.TryGetValue("userId", out var userIdValue))
        {
            Console.WriteLine("DELETE AFTER TESTING");
            Console.WriteLine($"userIdValue test");
            Console.WriteLine("DELETE AFTER TESTING");
            return Result<LoginCommandResponse>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        if (!claims.TryGetValue(ClaimTypes.Email, out var email))
        {
            Console.WriteLine("DELETE AFTER TESTING");
            Console.WriteLine($"email test");
            Console.WriteLine("DELETE AFTER TESTING");
            return Result<LoginCommandResponse>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        if (!claims.TryGetValue("codeHash", out var codeHash))
        {
            Console.WriteLine("DELETE AFTER TESTING");
            Console.WriteLine($"codeHash test");
            Console.WriteLine("DELETE AFTER TESTING");
            return Result<LoginCommandResponse>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            Console.WriteLine("DELETE AFTER TESTING");
            Console.WriteLine($"userId parse test");
            Console.WriteLine("DELETE AFTER TESTING");
            return Result<LoginCommandResponse>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        if (!_emailTokenService.VerifyToken(request.Code, codeHash))
        {
            Console.WriteLine("DELETE AFTER TESTING");
            Console.WriteLine($"Second test");
            Console.WriteLine("DELETE AFTER TESTING");
            return Result<LoginCommandResponse>.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null || !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return Result<LoginCommandResponse>.Failure(Error.Unauthorized(ErrorCodes.UserNotFound));
        }

        user.RecordSuccessfulLogin();

        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, []);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken(
            user.Id,
            _emailTokenService.HashToken(refreshTokenValue),
            DateTime.UtcNow.AddDays(AuthConstants.RefreshTokenExpirationDays),
            request.IpAddress,
            request.UserAgent);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.RefreshTokens.CreateAsync(refreshToken, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (DbException e)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result<LoginCommandResponse>.Failure(Error.InternalError(e.Message));
        }

        await _publishEndpoint.Publish(new UserLoggedInEvent
        {
            UserId = user.Id,
            IpAddress = request.IpAddress ?? string.Empty,
            UserAgent = request.UserAgent ?? string.Empty,
            LoginAt = DateTime.UtcNow
        }, cancellationToken);

        return Result<LoginCommandResponse>.Success(new LoginCommandResponse
        {
            UserId = user.Id,
            Email = user.Email,
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            AccessTokenExpiresIn = AuthConstants.AccessTokenExpirationMinutes * 60,
            RequiresMfa = false,
            MfaChallenge = null,
            MfaMethod = null
        });
    }
}

/// <summary>
/// Handles refresh token command.
/// Validates refresh token and returns new access token (with token rotation).
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IEmailTokenService _emailTokenService;
    private readonly IPublishEndpoint _publishEndpoint;
    // TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE

    public RefreshTokenCommandHandler(
        IAuthUnitOfWork unitOfWork,
        ITokenService tokenService,
        IEmailTokenService emailTokenService,
        IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _emailTokenService = emailTokenService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE
        // 1. Find refresh token hash in database
        // 2. Check for token reuse attack (was it already revoked?)
        // 3. Validate token expiration
        // 4. Get the associated user
        // 5. Create new access token
        // 6. Create new refresh token (rotation)
        // 7. Revoke old refresh token
        // 8. Save new token to database
        // 9. Return new tokens
        // 10. Handle token reuse: publish SecurityAlert event, revoke all user tokens

        var tokenHash = _emailTokenService.HashToken(request.RefreshToken);
        var refreshToken = await _unitOfWork.RefreshTokens.GetByHashAsync(tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return Result<RefreshTokenResponse>.Failure(Error.Unauthorized(ErrorCodes.InvalidRefreshToken));
        }

        if (refreshToken.HasBeenReused())
        {
            await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(
                refreshToken.UserId,
                ErrorCodes.RefreshTokenReuse,
                cancellationToken);

            await _publishEndpoint.Publish(new SessionRevokedEvent
            {
                UserId = refreshToken.UserId,
                TokenId = refreshToken.Id,
                Reason = ErrorCodes.RefreshTokenReuse,
                RevokedAt = DateTime.UtcNow
            }, cancellationToken);

            return Result<RefreshTokenResponse>.Failure(Error.Unauthorized(ErrorCodes.RefreshTokenReuse));
        }

        if (refreshToken.IsExpired)
        {
            return Result<RefreshTokenResponse>.Failure(Error.Unauthorized(ErrorCodes.TokenExpired));
        }

        var user = await _unitOfWork.Users.GetByIdAsync(refreshToken.UserId, cancellationToken);

        if (user is null)
        {
            return Result<RefreshTokenResponse>.Failure(Error.Unauthorized(ErrorCodes.InvalidRefreshToken));
        }

        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, []);
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        var newRefreshToken = new RefreshToken(
            user.Id,
            _emailTokenService.HashToken(newRefreshTokenValue),
            DateTime.UtcNow.AddDays(AuthConstants.RefreshTokenExpirationDays),
            request.IpAddress,
            request.UserAgent);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            refreshToken.MarkAsRotated(newRefreshToken.Id);
            await _unitOfWork.RefreshTokens.UpdateAsync(refreshToken, cancellationToken);
            await _unitOfWork.RefreshTokens.CreateAsync(newRefreshToken, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (DbException e)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result<RefreshTokenResponse>.Failure(Error.InternalError(e.Message));
        }

        await _publishEndpoint.Publish(new RefreshTokenRotatedEvent
        {
            UserId = user.Id,
            TokenId = newRefreshToken.Id,
            RotatedAt = DateTime.UtcNow,
            ExpiresAt = newRefreshToken.ExpiresAt
        }, cancellationToken);

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenValue,
            AccessTokenExpiresIn = AuthConstants.AccessTokenExpirationMinutes * 60
        });
    }
}

/// <summary>
/// Handles email verification command.
/// Verifies email and marks user as verified.
/// </summary>
public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result>
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly IEmailTokenService _emailTokenService;
    private readonly IPublishEndpoint _publishEndpoint;
    // TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE

    public VerifyEmailCommandHandler(
        IAuthUnitOfWork unitOfWork,
        IEmailTokenService emailTokenService,
        IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _emailTokenService = emailTokenService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        // TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE
        // 1. Find verification token by hash
        // 2. Check if token is valid (not expired, not used)
        // 3. Verify email matches
        // 4. Mark token as verified
        // 5. Mark user as email verified
        // 6. Save changes
        var tokenHash = _emailTokenService.HashToken(request.Token);
        var verificationToken = await _unitOfWork.EmailVerificationTokens.GetByHashAsync(tokenHash, cancellationToken);

        if (verificationToken is null)
        {
            await _publishEndpoint.Publish(new EmailVerificationAttemptedEvent
            {
                UserId = request.UserId,
                Email = request.Email,
                Success = false,
                AttemptedAt = DateTime.UtcNow,
                Source = "AuthService.Application"
            }, cancellationToken);

            return Result.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        if (verificationToken.UserId != request.UserId ||
            !string.Equals(verificationToken.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            await _publishEndpoint.Publish(new EmailVerificationAttemptedEvent
            {
                UserId = request.UserId,
                Email = request.Email,
                Success = false,
                AttemptedAt = DateTime.UtcNow,
                Source = "AuthService.Application"
            }, cancellationToken);

            return Result.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        if (!verificationToken.IsValid)
        {
            await _publishEndpoint.Publish(new EmailVerificationAttemptedEvent
            {
                UserId = request.UserId,
                Email = request.Email,
                Success = false,
                AttemptedAt = DateTime.UtcNow,
                Source = "AuthService.Application"
            }, cancellationToken);

            return Result.Failure(Error.Unauthorized(ErrorCodes.TokenExpired));
        }

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            await _publishEndpoint.Publish(new EmailVerificationAttemptedEvent
            {
                UserId = request.UserId,
                Email = request.Email,
                Success = false,
                AttemptedAt = DateTime.UtcNow,
                Source = "AuthService.Application"
            }, cancellationToken);

            return Result.Failure(Error.Unauthorized(ErrorCodes.UserNotFound));
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            verificationToken.MarkAsVerified();
            user.VerifyEmail();

            await _unitOfWork.EmailVerificationTokens.UpdateAsync(verificationToken, cancellationToken);
            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (DbException e)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result.Failure(Error.InternalError(e.Message));
        }

        await _publishEndpoint.Publish(new EmailVerificationAttemptedEvent
        {
            UserId = user.Id,
            Email = user.Email,
            Success = true,
            AttemptedAt = DateTime.UtcNow,
            Source = "AuthService.Application"
        }, cancellationToken);

        return Result.Success();
    }
}

/// <summary>
/// Handles password reset request.
/// Sends reset token to user's email.
/// </summary>
public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Result>
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly IEmailTokenService _emailTokenService;
    private readonly IEmailService _emailService;
    // TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE

    public RequestPasswordResetCommandHandler(
        IAuthUnitOfWork unitOfWork,
        IEmailTokenService emailTokenService,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _emailTokenService = emailTokenService;
        _emailService = emailService;
    }

    public async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        // TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE
        // 1. Find user by email
        // 2. Generate password reset token
        // 3. Save token to database
        // 4. Send email with reset link
        // 5. Return success (don't reveal whether user exists for security)

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            return Result.Success();
        }

        var resetTokenValue = _emailTokenService.GenerateToken();
        var resetToken = new PasswordResetToken(
            user.Id,
            _emailTokenService.HashToken(resetTokenValue),
            DateTime.UtcNow.AddMinutes(60),
            request.IpAddress);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            await _unitOfWork.PasswordResetTokens.CreateAsync(resetToken, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (DbException e)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result.Failure(Error.InternalError(e.Message));
        }

        await _emailService.SendPasswordResetAsync(user.Email, resetTokenValue, cancellationToken);

        return Result.Success();
    }
}

/// <summary>
/// Handles password reset with token.
/// Validates token and updates user password.
/// </summary>
public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailTokenService _emailTokenService;
    // TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE

    public ResetPasswordCommandHandler(
        IAuthUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEmailTokenService emailTokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _emailTokenService = emailTokenService;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE
        // 1. Find reset token by hash
        // 2. Check if token is valid
        // 3. Check attempt count (brute force protection)
        // 4. Find user
        // 5. Hash new password
        // 6. Update user password
        // 7. Mark token as used
        // 8. Save changes
        // 9. Revoke all user sessions (for security)
        // 10. Publish PasswordChangedEvent

        var tokenHash = _emailTokenService.HashToken(request.Token);
        var resetToken = await _unitOfWork.PasswordResetTokens.GetByHashAsync(tokenHash, cancellationToken);

        if (resetToken is null)
        {
            return Result.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        if (resetToken.UserId != request.UserId)
        {
            resetToken.IncrementAttempt();
            await _unitOfWork.PasswordResetTokens.UpdateAsync(resetToken, cancellationToken);

            return Result.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        if (resetToken.HasExceededAttempts)
        {
            return Result.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        if (!resetToken.IsValid)
        {
            resetToken.IncrementAttempt();
            await _unitOfWork.PasswordResetTokens.UpdateAsync(resetToken, cancellationToken);

            return Result.Failure(Error.Unauthorized(ErrorCodes.TokenExpired));
        }

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            resetToken.IncrementAttempt();
            await _unitOfWork.PasswordResetTokens.UpdateAsync(resetToken, cancellationToken);

            return Result.Failure(Error.Unauthorized(ErrorCodes.InvalidToken));
        }

        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            user.UpdatePassword(newPasswordHash);
            resetToken.MarkAsUsed();

            await _unitOfWork.Users.UpdateAsync(user, cancellationToken);
            await _unitOfWork.PasswordResetTokens.UpdateAsync(resetToken, cancellationToken);
            await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(
                user.Id,
                "password_changed",
                cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (DbException e)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result.Failure(Error.InternalError(e.Message));
        }

        return Result.Success();
    }
}

/// <summary>
/// Handles logout command.
/// Revokes user's refresh token(s).
/// </summary>
public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IAuthUnitOfWork _unitOfWork;
    private readonly IEmailTokenService _emailTokenService;
    private readonly IPublishEndpoint _publishEndpoint;
    // TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE

    public LogoutCommandHandler(IAuthUnitOfWork unitOfWork, IEmailTokenService emailTokenService, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _emailTokenService = emailTokenService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE
        // 1. If refresh token value provided, revoke just that token
        // 2. Otherwise, if token ID provided, revoke just that token
        // 3. Otherwise, revoke all user tokens
        // 4. Save changes
        var reason = request.Reason ?? "logout";
        Guid? revokedTokenId = null;

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                var tokenHash = _emailTokenService.HashToken(request.RefreshToken);
                var refreshToken = await _unitOfWork.RefreshTokens.GetByHashAsync(tokenHash, cancellationToken);

                if (refreshToken is not null && refreshToken.UserId == request.UserId)
                {
                    refreshToken.Revoke(reason);
                    revokedTokenId = refreshToken.Id;
                    await _unitOfWork.RefreshTokens.UpdateAsync(refreshToken, cancellationToken);
                }
            }
            else if (request.RefreshTokenId is not null)
            {
                var refreshToken = await _unitOfWork.RefreshTokens.GetByIdAsync(request.RefreshTokenId.Value, cancellationToken);

                if (refreshToken is not null && refreshToken.UserId == request.UserId)
                {
                    refreshToken.Revoke(reason);
                    revokedTokenId = refreshToken.Id;
                    await _unitOfWork.RefreshTokens.UpdateAsync(refreshToken, cancellationToken);
                }
            }
            else
            {
                await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(request.UserId, reason, cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (DbException e)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result.Failure(Error.InternalError(e.Message));
        }

        await _publishEndpoint.Publish(new SessionRevokedEvent
        {
            UserId = request.UserId,
            TokenId = revokedTokenId ?? request.RefreshTokenId,
            Reason = reason,
            RevokedAt = DateTime.UtcNow,
            Source = "AuthService.Application"
        }, cancellationToken);

        return Result.Success();
    }
}