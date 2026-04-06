namespace AuthService.Domain.Entities;

/// <summary>
/// Represents a refresh token used for obtaining new access tokens.
/// Implements refresh token rotation for security best practices.
/// </summary>
public class RefreshToken
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
    /// Hash of the token (never store plain token in DB for security).
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;
    
    /// <summary>
    /// When the token was issued (UTC).
    /// </summary>
    public DateTime IssuedAt { get; private set; }
    
    /// <summary>
    /// When the token expires (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; private set; }
    
    /// <summary>
    /// When the token was revoked/rotated (UTC).
    /// Null if still active.
    /// </summary>
    public DateTime? RevokedAt { get; private set; }
    
    /// <summary>
    /// Reason for revocation (e.g., "rotated", "logout", "suspicious_activity").
    /// </summary>
    public string? RevocationReason { get; private set; }
    
    /// <summary>
    /// If this token was rotated, this points to the new token ID.
    /// Helps detect token reuse attacks.
    /// </summary>
    public Guid? RotatedToTokenId { get; private set; }
    
    /// <summary>
    /// IP address of the client that issued this token.
    /// Used for anomaly detection.
    /// </summary>
    public string? IssuedFromIpAddress { get; private set; }
    
    /// <summary>
    /// User-Agent of the client that issued this token.
    /// Used for anomaly detection.
    /// </summary>
    public string? IssuedFromUserAgent { get; private set; }
    
    /// <summary>
    /// Is the token still active (not revoked and not expired)?
    /// </summary>
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
    
    /// <summary>
    /// Has the token expired?
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public RefreshToken()
    {
    }

    public RefreshToken(
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        string? issuedFromIpAddress = null,
        string? issuedFromUserAgent = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        IssuedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        IssuedFromIpAddress = issuedFromIpAddress;
        IssuedFromUserAgent = issuedFromUserAgent;
    }

    /// <summary>
    /// Revokes the token with a reason.
    /// </summary>
    public void Revoke(string reason)
    {
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;
    }

    /// <summary>
    /// Marks this token as rotated and points to the new token ID.
    /// </summary>
    public void MarkAsRotated(Guid newTokenId)
    {
        Revoke("rotated");
        RotatedToTokenId = newTokenId;
    }

    /// <summary>
    /// Detects if this token has been reused (security anomaly).
    /// A reused token is one that was already revoked but is being used again.
    /// </summary>
    public bool HasBeenReused()
    {
        // If token is revoked but someone is trying to use it, it's a reuse attempt
        return RevokedAt != null;
    }
}
