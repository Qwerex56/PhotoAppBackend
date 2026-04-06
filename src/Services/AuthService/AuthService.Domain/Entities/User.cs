namespace AuthService.Domain.Entities;

/// <summary>
/// Represents a user in the authentication system.
/// This entity is owned by AuthService and contains authentication-specific data.
/// User profile and additional details are stored in UserManagementService.
/// </summary>
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    
    /// <summary>
    /// Password hash using Argon2id algorithm.
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;
    
    /// <summary>
    /// When the user registered (UTC).
    /// </summary>
    public DateTime RegisteredAt { get; private set; }
    
    /// <summary>
    /// When the password was last changed (UTC).
    /// </summary>
    public DateTime? PasswordChangedAt { get; private set; }
    
    /// <summary>
    /// Is the email verified? Required to use most features.
    /// </summary>
    public bool EmailVerified { get; private set; }
    
    /// <summary>
    /// When the email was verified (UTC).
    /// </summary>
    public DateTime? EmailVerifiedAt { get; private set; }
    
    /// <summary>
    /// Is the account locked (multiple failed login attempts)?
    /// </summary>
    public bool IsLocked { get; private set; }
    
    /// <summary>
    /// When the account was locked (UTC).
    /// </summary>
    public DateTime? LockedAt { get; private set; }
    
    /// <summary>
    /// Count of failed login attempts (reset on successful login).
    /// </summary>
    public int FailedLoginAttempts { get; private set; }
    
    /// <summary>
    /// When the last login occurred (UTC).
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }
    
    /// <summary>
    /// Active refresh tokens for this user.
    /// </summary>
    public List<RefreshToken> RefreshTokens { get; } = [];

    public User()
    {
    }

    public User(Guid id, string email, string passwordHash)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        RegisteredAt = DateTime.UtcNow;
        EmailVerified = false;
        IsLocked = false;
        FailedLoginAttempts = 0;
    }

    /// <summary>
    /// Records a failed login attempt and locks the account if threshold exceeded.
    /// TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE
    /// </summary>
    public void RecordFailedLoginAttempt()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5) // Configurable threshold
        {
            LockAccount();
        }
    }

    /// <summary>
    /// Records a successful login and resets failed attempts counter.
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LastLoginAt = DateTime.UtcNow;
        if (IsLocked)
        {
            IsLocked = false;
            LockedAt = null;
        }
    }

    /// <summary>
    /// Locks the account to prevent further login attempts.
    /// </summary>
    public void LockAccount()
    {
        IsLocked = true;
        LockedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unlocks the account and resets failed login attempts.
    /// </summary>
    public void UnlockAccount()
    {
        IsLocked = false;
        LockedAt = null;
        FailedLoginAttempts = 0;
    }

    /// <summary>
    /// Marks the email as verified.
    /// </summary>
    public void VerifyEmail()
    {
        EmailVerified = true;
        EmailVerifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the password hash and records the change time.
    /// </summary>
    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        PasswordChangedAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
    }
}
