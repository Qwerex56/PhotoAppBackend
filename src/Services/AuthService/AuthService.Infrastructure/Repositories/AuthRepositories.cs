namespace AuthService.Infrastructure.Repositories;

using Domain.Entities;
using Domain.Repositories;
using Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Implementation of IUserRepository for database persistence.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AuthServiceDbContext _context;

    public UserRepository(AuthServiceDbContext context)
    {
        _context = context;
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Implementation of IRefreshTokenRepository for database persistence.
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthServiceDbContext _context;

    public RefreshTokenRepository(AuthServiceDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<RefreshToken?> GetByIdAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == tokenId, cancellationToken);
    }

    public async Task<List<RefreshToken>> GetActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<RefreshToken> UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(reason);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        _context.RefreshTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Implementation of IEmailVerificationTokenRepository for database persistence.
/// </summary>
public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly AuthServiceDbContext _context;

    public EmailVerificationTokenRepository(AuthServiceDbContext context)
    {
        _context = context;
    }

    public async Task<EmailVerificationToken> CreateAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        await _context.EmailVerificationTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<EmailVerificationToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<EmailVerificationToken?> GetPendingTokenAsync(Guid userId, string email, CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerificationTokens
            .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.Email == email &&
                t.VerifiedAt == null &&
                t.ExpiresAt > DateTime.UtcNow,
                cancellationToken);
    }

    public async Task<EmailVerificationToken> UpdateAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        _context.EmailVerificationTokens.Update(token);
        await _context.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task DeleteExpiredOrUsedTokensAsync(CancellationToken cancellationToken = default)
    {
        var tokensToDelete = await _context.EmailVerificationTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow || t.VerifiedAt != null)
            .ToListAsync(cancellationToken);

        _context.EmailVerificationTokens.RemoveRange(tokensToDelete);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Implementation of IPasswordResetTokenRepository for database persistence.
/// </summary>
public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AuthServiceDbContext _context;

    public PasswordResetTokenRepository(AuthServiceDbContext context)
    {
        _context = context;
    }

    public async Task<PasswordResetToken> CreateAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        await _context.PasswordResetTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<PasswordResetToken?> GetActiveTokenForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t =>
                t.UserId == userId &&
                t.UsedAt == null &&
                t.ExpiresAt > DateTime.UtcNow,
                cancellationToken);
    }

    public async Task<PasswordResetToken> UpdateAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        _context.PasswordResetTokens.Update(token);
        await _context.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task DeleteExpiredOrUsedTokensAsync(CancellationToken cancellationToken = default)
    {
        var tokensToDelete = await _context.PasswordResetTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow || t.UsedAt != null)
            .ToListAsync(cancellationToken);

        _context.PasswordResetTokens.RemoveRange(tokensToDelete);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Unit of Work implementation for AuthService.
/// Coordinates multiple repositories in a single transaction.
/// </summary>
public class AuthUnitOfWork : IAuthUnitOfWork
{
    private readonly AuthServiceDbContext _context;
    private IUserRepository? _userRepository;
    private IRefreshTokenRepository? _refreshTokenRepository;
    private IEmailVerificationTokenRepository? _emailVerificationTokenRepository;
    private IPasswordResetTokenRepository? _passwordResetTokenRepository;

    public AuthUnitOfWork(AuthServiceDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _userRepository ??= new UserRepository(_context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokenRepository ??= new RefreshTokenRepository(_context);
    public IEmailVerificationTokenRepository EmailVerificationTokens => _emailVerificationTokenRepository ??= new EmailVerificationTokenRepository(_context);
    public IPasswordResetTokenRepository PasswordResetTokens => _passwordResetTokenRepository ??= new PasswordResetTokenRepository(_context);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Database.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _context.Database.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.RollbackTransactionAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }
    }
}
