# Shared Libraries Documentation

## 📦 Shared.Contracts

Contains all event, command, and query definitions used for inter-service communication and application command orchestration.

### Events (Shared.Contracts/Events)

Base interface all events must implement:

```csharp
public interface IDomainEvent
{
    Guid CorrelationId { get; }        // Trace events across services
    DateTime OccurredAt { get; }       // When event happened
    string Source { get; }              // Which service published it
}
```

#### Auth Events

**UserRegisteredEvent**
- Published when: User completes registration
- Consumed by: UserManagement, Device, Audit services
- Data: UserId, Email, FullName, RegisteredAt

**UserLoggedInEvent**
- Published when: User successfully authenticates
- Consumed by: Device, Audit services
- Data: UserId, IpAddress, UserAgent, LoginAt
- Note: Contains client info for anomaly detection

**RefreshTokenRotatedEvent**
- Published when: Refresh token is rotated
- Consumed by: Audit, Device services
- Data: UserId, TokenId, RotatedAt, ExpiresAt

**SessionRevokedEvent**
- Published when: User logs out or suspicious activity detected
- Consumed by: Audit, Device, UserManagement
- Data: UserId, TokenId (nullable), Reason, RevokedAt
- Reasons: "logout", "suspicious_activity", "admin_revoke", etc.

**EmailVerificationAttemptedEvent**
- Published when: Email verification is attempted
- Consumed by: Audit service
- Data: UserId, Email, Success, AttemptedAt

#### User Events

**UserProfileUpdatedEvent**
- Published when: User updates profile info
- Consumed by: Audit service
- Data: UserId, FullName, Avatar, Bio, UpdatedAt

**UserDeletedEvent**
- Published when: User account is deleted
- Consumed by: Media, Device, Audit services
- Data: UserId, Email, DeletedAt, Reason

**UserMfaChangedEvent**
- Published when: MFA settings change
- Consumed by: Audit service
- Data: UserId, Method, Enabled, ChangedAt
- Methods: "webauthn", "totp", "email"

**UserRoleChangedEvent**
- Published when: User role is assigned/removed
- Consumed by: UserManagement, Audit
- Data: UserId, RoleName, Assigned, ChangedAt

### Commands (Shared.Contracts/Commands)

Base interface for all commands:

```csharp
public interface ICommand
{
    Guid CorrelationId { get; }  // Track command execution
}

public interface ICommand<out TResponse> : ICommand { }
```

Handled by MediatR in application layer.

### Queries (Shared.Contracts/Queries)

Base interface for read operations:

```csharp
public interface IQuery<out TResponse>
{
    Guid CorrelationId { get; }
}
```

---

## 📊 Shared.Results

Provides functional error handling without exceptions for expected failures.

### Error Class

```csharp
public sealed record Error
{
    public string Code { get; }      // Error code (e.g., "AUTH_INVALID_CREDENTIALS")
    public string Description { get; } // Human-readable message
}
```

**Predefined Error Factories**:

```csharp
Error.ValidationError("details")      // VALIDATION_ERROR
Error.NotFound("resource")             // NOT_FOUND
Error.Unauthorized("details")          // UNAUTHORIZED
Error.Forbidden("details")             // FORBIDDEN
Error.Conflict("details")              // CONFLICT
Error.InternalError("details")         // INTERNAL_ERROR
Error.BadRequest("details")            // BAD_REQUEST
```

### Result<T> Class

Represents success with value OR failure with error.

```csharp
// Success case
var result = Result<User>.Success(user);

// Failure case
var result = Result<User>.Failure(Error.NotFound("User"));

// Check result
if (result.IsSuccess)
{
    var user = result.Value; // Safe to access
}

// Or use OnSuccess/OnFailure callbacks
result
    .OnSuccess(user => Log.Information("User found: {Email}", user.Email))
    .OnFailure(error => Log.Warning("Error: {ErrorCode}", error.Code));

// Or use functional composition
var getUserRoles = result
    .Then(user => GetUserRoles(user.Id))
    .Map(roles => new UserDto { UserId = result.Value.Id, Roles = roles });
```

### Result Class

For operations with no return value.

```csharp
// Success
return Result.Success();

// Failure
return Result.Failure(Error.Unauthorized());

// Convert to Result<T>
var result = Result.Success()
    .ToResult(defaultUser); // Result<UserProfile>
```

**Benefits**:
- No exception throwing for expected failures
- Type-safe error handling
- Composable with LINQ-like methods
- Reduced code complexity

---

## 🔢 Shared.Constants

Global constants used across all services.

### AuthConstants

```csharp
public static class AuthConstants
{
    // JWT Configuration
    const int AccessTokenExpirationMinutes = 15;
    const int RefreshTokenExpirationDays = 7;
    const string JwtClaimSubject = "sub";
    
    // Password Requirements (OWASP)
    const int MinimumPasswordLength = 12;
    const int PasswordHashIterations = 4;           // Argon2id
    const int PasswordHashMemorySize = 65536;       // 64 MB
    const int PasswordHashParallelism = 2;
}
```

Use in code:

```csharp
string role = AuthConstants.JwtClaimRoles;
var minLength = AuthConstants.MinimumPasswordLength;
```

### ErrorCodes

Standard error codes for consistent error handling:

```csharp
// Authentication
ErrorCodes.InvalidCredentials              // AUTH_INVALID_CREDENTIALS
ErrorCodes.UserNotFound                    // AUTH_USER_NOT_FOUND
ErrorCodes.UserAlreadyExists               // AUTH_USER_ALREADY_EXISTS
ErrorCodes.InvalidToken                    // AUTH_INVALID_TOKEN
ErrorCodes.TokenExpired                    // AUTH_TOKEN_EXPIRED
ErrorCodes.RefreshTokenReuse               // AUTH_REFRESH_TOKEN_REUSE
ErrorCodes.EmailNotVerified                // AUTH_EMAIL_NOT_VERIFIED
ErrorCodes.WeakPassword                    // AUTH_WEAK_PASSWORD

// User Management
ErrorCodes.UserProfileNotFound             // USER_PROFILE_NOT_FOUND
ErrorCodes.InvalidRole                     // USER_INVALID_ROLE
ErrorCodes.RoleAlreadyAssigned             // USER_ROLE_ALREADY_ASSIGNED

// MFA
ErrorCodes.MfaRequired                     // MFA_REQUIRED
ErrorCodes.InvalidMfaCode                  // MFA_INVALID_CODE
```

### CacheKeys

Standardized Redis key patterns:

```csharp
// Pattern-based keys
string userKey = CacheKeys.User(userId);
string emailKey = CacheKeys.UserByEmail(email);
string tokenBlacklist = CacheKeys.RefreshTokenBlacklist(tokenId);
string roles = CacheKeys.Roles(userId);

// For deletion
var pattern = $"photoapp:user:{userId}:*";  // Wildcard pattern
```

### RoleNames

System role definitions:

```csharp
RoleNames.Admin       // "Admin"
RoleNames.User        // "User"
RoleNames.Moderator   // "Moderator"
```

### MessageQueues

Message queue and topic names:

```csharp
// Service queues (for direct commands)
MessageQueues.AuthServiceQueue              // "photo-app.auth-service"
MessageQueues.UserServiceQueue              // "photo-app.user-management-service"

// Event topics (for pub/sub)
MessageQueues.AuthEventsTopic               // "photo-app.auth.events"
MessageQueues.UserEventsTopic               // "photo-app.user.events"
```

### PolicyNames

Authorization policy names:

```csharp
PolicyNames.RequireAuthentication   // "RequireAuthentication"
PolicyNames.RequireAdmin            // "RequireAdmin"
PolicyNames.RequireEmailVerified    // "RequireEmailVerified"
PolicyNames.RequireMfa              // "RequireMfa"
```

---

## 🎯 Best Practices for Shared Libraries

### Adding New Constants

1. Group related constants in a static class
2. Use clear, descriptive names
3. Add XML documentation
4. Avoid magic numbers - move to constants

```csharp
/// <summary>
/// Maximum failed login attempts before account lock.
/// </summary>
public const int MaxFailedLoginAttempts = 5;
```

### Adding New Events

1. Inherit from `DomainEvent`
2. Make properties immutable (init-only)
3. Add XML doc with consumer list
4. Include all necessary context data

```csharp
/// <summary>
/// Published when a user accepts a friend request.
/// Consumed by: NotificationService, UserManagementService
/// </summary>
public class FriendRequestAcceptedEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public Guid FriendId { get; init; }
    public DateTime AcceptedAt { get; init; }
}
```

### Adding New Commands

1. Inherit from `Command` or `Command<T>`
2. Use immutable properties
3. Include validation hints in XML documentation

```csharp
/// <summary>
/// Updates user profile information.
/// Validation: FullName max 255 chars, Bio max 1000 chars
/// </summary>
public class UpdateProfileCommand : Command<Result>
{
    public Guid UserId { get; init; }
    public string? FullName { get; init; }
    public string? Bio { get; init; }
}
```

---

## 📦 Dependency Management

### Shared Libraries NuGet Dependencies

Only add dependencies that:
- Are essential for contracts/DTOs
- Have no business logic
- Are widely compatible
- Approved by architecture team

Current approved packages:
- System namespaces (Collections, LINQ, etc.)
- No external dependencies (keep it lean)

### Service-Specific Dependencies

Each service layer has distinct responsibilities:

**Domain Layer**:
- No external dependencies
- Only System namespaces
- Pure business logic

**Application Layer**:
- MediatR (orchestration)
- FluentValidation
- MassTransit (events)

**Infrastructure Layer**:
- Entity Framework Core
- Data access
- External service integrations

**API Layer**:
- ASP.NET Core
- Authentication/Authorization
- Logging (Serilog)

---

## 🔄 Versioning Strategy

### Event Versioning

As requirements change, maintain backward compatibility:

```csharp
// V1 - Original
public class UserCreatedEventV1 : DomainEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; }
}

// V2 - Extended with phone
public class UserCreatedEventV2 : DomainEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; }
    public string? PhoneNumber { get; init; }
}

// Consumers handle both versions
public class UserProfileCreatedConsumer : 
    IConsumer<UserCreatedEventV1>,
    IConsumer<UserCreatedEventV2>
{
    // Version detection and appropriate handling
}
```

---

## 📝 Documentation Requirements

### For Events

```csharp
/// <summary>
/// [ONE LINE DESCRIPTION]
/// </summary>
/// <remarks>
/// Consumed by: ServiceA, ServiceB
/// Published when: [CONDITION]
/// Failure impact: [What if consumer crashes?]
/// </remarks>
```

### For Commands

```csharp
/// <summary>
/// [ONE LINE DESCRIPTION]
/// </summary>
/// <remarks>
/// Validation: [LIST KEY VALIDATIONS]
/// Returns: [WHAT SUCCESS LOOKS LIKE]
/// Failure codes: [POSSIBLE ERROR.CODE VALUES]
/// </remarks>
```

---

**Last Updated**: 4 kwietnia 2026  
**Version**: 1.0
