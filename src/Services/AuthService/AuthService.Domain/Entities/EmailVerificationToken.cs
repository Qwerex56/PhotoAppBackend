namespace AuthService.Domain.Entities;

/// <summary>
/// Represents an email verification token sent to user during registration.
/// Single-use token with expiration.
/// </summary>
public class EmailVerificationToken
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
    /// Email address being verified.
    /// </summary>
    public string Email { get; private set; } = string.Empty;
    
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
    /// </summary>
    public DateTime ExpiresAt { get; private set; }
    
    /// <summary>
    /// When the email was verified (UTC).
    /// Null if not yet verified.
    /// </summary>
    public DateTime? VerifiedAt { get; private set; }
    
    /// <summary>
    /// IP address that requested the verification.
    /// Used for security monitoring.
    /// </summary>
    public string? RequestedFromIpAddress { get; private set; }

    public EmailVerificationToken()
    {
    }

    public EmailVerificationToken(
        Guid userId,
        string email,
        string tokenHash,
        DateTime expiresAt,
        string? requestedFromIpAddress = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Email = email;
        TokenHash = tokenHash;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        RequestedFromIpAddress = requestedFromIpAddress;
    }

    /// <summary>
    /// Marks the email as verified.
    /// </summary>
    public void MarkAsVerified()
    {
        VerifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the token is still valid (not expired and not used).
    /// </summary>
    public bool IsValid => DateTime.UtcNow < ExpiresAt && VerifiedAt == null;

    /// <summary>
    /// Checks if the token has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Checks if the token has already been used.
    /// </summary>
    public bool IsUsed => VerifiedAt != null;
}
