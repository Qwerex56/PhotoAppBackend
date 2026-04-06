namespace AuthService.Domain.Entities;

/// <summary>
/// Represents a password reset token sent to user email.
/// Single-use token with short expiration time.
/// </summary>
public class PasswordResetToken
{
    public Guid Id { get; private set; }
    
    /// <summary>
    /// Foreign key to the User entity.
    /// </summary>
    public Guid UserId { get; private set; }
    
    /// <summary>
    /// User navigation property.
    /// </summary>
    public User User { get; private set; } = null!;
    
    /// <summary>
    /// Hash of the token (never store plain token in DB).
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;
    
    /// <summary>
    /// When the token was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// When the token expires (UTC).
    /// Password reset tokens have shorter TTL than email verification.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }
    
    /// <summary>
    /// When the password was reset using this token (UTC).
    /// Null if not yet used.
    /// </summary>
    public DateTime? UsedAt { get; private set; }
    
    /// <summary>
    /// IP address of the client requesting the reset.
    /// Used for security monitoring and anomaly detection.
    /// </summary>
    public string? RequestedFromIpAddress { get; private set; }
    
    /// <summary>
    /// How many times this token was attempted (to detect brute force).
    /// </summary>
    public int AttemptCount { get; private set; }

    public PasswordResetToken()
    {
    }

    public PasswordResetToken(
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        string? requestedFromIpAddress = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        RequestedFromIpAddress = requestedFromIpAddress;
        AttemptCount = 0;
    }

    /// <summary>
    /// Marks the token as used when password is successfully reset.
    /// </summary>
    public void MarkAsUsed()
    {
        UsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments the attempt counter.
    /// </summary>
    public void IncrementAttempt()
    {
        AttemptCount++;
    }

    /// <summary>
    /// Checks if the token is still valid (not expired and not used).
    /// </summary>
    public bool IsValid => DateTime.UtcNow < ExpiresAt && UsedAt == null;

    /// <summary>
    /// Checks if the token has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Checks if the token has already been used.
    /// </summary>
    public bool IsUsed => UsedAt != null;

    /// <summary>
    /// Checks if too many attempts have been made (brute force detection).
    /// </summary>
    public bool HasExceededAttempts => AttemptCount > 5;
}
