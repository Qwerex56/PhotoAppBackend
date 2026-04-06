namespace AuthService.Domain.Repositories;

using Entities;

/// <summary>
/// Repository interface for User entity.
/// Defines all data access patterns for users.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Creates a new user in the repository.
    /// </summary>
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by ID.
    /// </summary>
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by email address.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user with the given email already exists.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user (soft delete recommended for audit trail).
    /// </summary>
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for RefreshToken entity.
/// Handles refresh token lifecycle and rotation.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    Task<RefreshToken> CreateAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a refresh token by ID.
    /// </summary>
    Task<RefreshToken?> GetByIdAsync(Guid tokenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active refresh tokens for a user.
    /// </summary>
    Task<List<RefreshToken>> GetActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a token by hash (for validation during refresh).
    /// </summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a refresh token (e.g., to mark as revoked).
    /// </summary>
    Task<RefreshToken> UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active tokens for a user (on logout or security event).
    /// </summary>
    Task RevokeAllUserTokensAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired tokens (cleanup operation).
    /// </summary>
    Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for EmailVerificationToken entity.
/// </summary>
public interface IEmailVerificationTokenRepository
{
    /// <summary>
    /// Creates a new email verification token.
    /// </summary>
    Task<EmailVerificationToken> CreateAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a token by hash.
    /// </summary>
    Task<EmailVerificationToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a pending verification token for a user and email.
    /// </summary>
    Task<EmailVerificationToken?> GetPendingTokenAsync(Guid userId, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a token (e.g., to mark as verified).
    /// </summary>
    Task<EmailVerificationToken> UpdateAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired or used tokens (cleanup).
    /// </summary>
    Task DeleteExpiredOrUsedTokensAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for PasswordResetToken entity.
/// </summary>
public interface IPasswordResetTokenRepository
{
    /// <summary>
    /// Creates a new password reset token.
    /// </summary>
    Task<PasswordResetToken> CreateAsync(PasswordResetToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a token by hash.
    /// </summary>
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an active token for a user.
    /// </summary>
    Task<PasswordResetToken?> GetActiveTokenForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a token (e.g., to mark as used or increment attempts).
    /// </summary>
    Task<PasswordResetToken> UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired or used tokens (cleanup).
    /// </summary>
    Task DeleteExpiredOrUsedTokensAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Unit of Work pattern for AUTH SERVICE data access.
/// Coordinates multiple repositories in a single transaction.
/// </summary>
public interface IAuthUnitOfWork : IDisposable, IAsyncDisposable
{
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IEmailVerificationTokenRepository EmailVerificationTokens { get; }
    IPasswordResetTokenRepository PasswordResetTokens { get; }

    /// <summary>
    /// Saves all changes made within the unit of work.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
