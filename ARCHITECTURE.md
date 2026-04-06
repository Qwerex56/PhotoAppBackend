# Architecture & Security Reference

## 🏛️ System Architecture

### Microservices Interaction

```
┌─────────────────────────────────────────────────────────────────┐
│                      API Gateway / Load Balancer                │
└────────────────────────────────────────────────────────────────┘
                    │                       │
        ┌───────────┘                       └──────────────┐
        │                                                   │
    ┌───▼──────────────┐                        ┌──────────▼────────┐
    │                  │                        │                   │
    │  AuthService     │                        │ UserManagement    │
    │  ┌────────────┐  │                        │ ┌──────────────┐  │
    │  │ JWT/Tokens │  │                        │ │ Profiles,    │  │
    │  │ Sessions   │  │                        │ │ Roles, Perms │  │
    │  └────────────┘  │                        │ └──────────────┘  │
    └────────┬─────────┘                        └────────┬──────────┘
             │                                           │
             └───────────┬───────────────────────────────┘
                         │
            ┌────────────┴────────────┐
            │                         │
     ┌──────▼──────┐         ┌────────▼──────┐
     │  PostgreSQL │         │    Redis      │
     │  (Multi-DB) │         │ (Cache/Cache) │
     └─────────────┘         └───────────────┘
                             
            All services connected via
            ┌──────────────────────────┐
            │ RabbitMQ Message Broker  │
            │ (MassTransit)            │
            └──────────────────────────┘
```

### Event Flow Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                          User Registration                        │
└──────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌────────────────────┐
                    │  AuthService       │
                    │  RegisterCommand   │
                    └────────────────────┘
                              │
                    ┌─────────┼─────────┐
                    ▼         ▼         ▼
        ┌──────────────┐  ┌──────┐  ┌────────────┐
        │ Create User  │  │Email │  │ Publish   │
        │ in AuthDB    │  │Send  │  │UserCreated│
        └──────────────┘  └──────┘  │Event      │
                                    └────────────┘
                                          │
                                          ▼
                    ┌─────────────────────────────────┐
                    │ RabbitMQ - UserCreatedEvent     │
                    └─────────────────────────────────┘
                                  │
                ┌─────────────────┼─────────────────┐
                ▼                 ▼                 ▼
        ┌────────────────┐ ┌────────────┐ ┌──────────────┐
        │User Management │ │ Audit      │ │ Device       │
        │Auto-create     │ │ Log Event  │ │ Log Login    │
        │Profile         │ │            │ │              │
        └────────────────┘ └────────────┘ └──────────────┘
```

---

## 🔐 Authentication Flow

### Login & Token Generation

```
1. User sends credentials
   POST /api/auth/login
   {
     "email": "user@example.com",
     "password": "SecurePass123!"
   }

2. AuthService processes:
   a) Find user by email
   b) Verify password against Argon2id hash
   c) Check if email is verified
   d) Check if account is locked
   e) Generate JWT access token (15-min TTL)
   f) Generate refresh token
   g) Hash and store refresh token
   h) Record successful login

3. Return tokens:
   {
     "userId": "guid",
     "accessToken": "eyJhbGc...",        // JWT
     "refreshToken": "secure_random...", // Stored as hash
     "accessTokenExpiresIn": 900,        // seconds
     "requiresMfa": false
   }

4. Store in client:
   - Access token: Memory (recreate on refresh)
   - Refresh token: HttpOnly Secure Cookie
```

### Token Refresh & Rotation

```
1. Access token expired, need new one
   POST /api/auth/refresh
   {
     "refreshToken": "client_token_value..."
   }

2. AuthService processes:
   a) Hash refresh token to find DB record
   b) Validate token is not revoked
   c) Validate token not expired
   d) Check for token reuse attack (was it already revoked?)
      - If yes: IMMEDIATELY revoke all user sessions (security alert!)
   e) Get user info
   f) Generate new access token
   g) Generate new refresh token
   h) Mark old refresh token as "rotated"
   i) Store new refresh token hash
   j) Save rotation event to audit log

3. Return new tokens:
   {
     "accessToken": "new_jwt...",
     "refreshToken": "new_secure_random...",
     "accessTokenExpiresIn": 900
   }

4. Repeat process...
```

### Logout & Revocation

```
1. User clicks logout
   POST /api/auth/logout

2. AuthService:
   a) Revoke current refresh token(s)
   b) Add token IDs to Redis blacklist
   c) Mark tokens as revoked in database

3. Next request with blacklisted token:
   - Token validation fails immediately (Redis check)
   - User redirected to login
```

---

## 🛡️ Security Measures

### Password Security

```
Registration Flow:
1. Validate password strength
   - Min 12 characters
   - Uppercase, lowercase, number, special char
2. Hash with Argon2id:
   - Iterations: 4
   - Memory: 64 MB
   - Parallelism: 2
3. Add pepper (from Key Vault)
4. Store hash (never plain text)

On Login:
1. Retrieve hash from database
2. Retrieve pepper from Key Vault
3. Hash provided password + pepper
4. Compare with stored hash
```

### Token Security

```
Access Token (JWT):
- Short TTL: 15 minutes
- Signed with RSA private key
- Contains: sub, email, roles, jti (token ID)
- Cannot be revoked (expiration handles this)
- Sent in Authorization header

Refresh Token:
- Stored as hash only
- Long TTL: 7 days
- Can be revoked/rotated
- Cannot access resources directly
- Only used to get new access token

Token Reuse Detection:
- Track all refresh token states
- If old token used after rotation: ALERT
- Revoke all user tokens immediately
- Log security event
```

### Multi-Factor Authentication (MFA) - Roadmap

```
WebAuthn/FIDO2 (Primary):
- Hardware key or biometric
- No secrets transmitted
- Phishing-resistant

TOTP (fallback):
- Time-based one-time password
- Stored encrypted in database
- Backup codes for account recovery
```

---

## 📊 Database Security

### Principles

1. **Defensive Indexing**
   - Indexes on foreign keys
   - Composite indexes for common queries
   - Partial indexes for soft deletes

2. **Data Sensitivity**
   - Password hashes: Argon2id
   - Token hashes: SHA-256
   - Sensitive data: PII encryption in future

3. **Audit Trail**
   - All security events logged
   - Append-only audit tables
   - Retention policy (7 years for compliance)

4. **Temporal Data**
   - All timestamps: UTC
   - Soft deletes: maintains history
   - No data truly deleted (GDPR exceptions)

---

## 🚨 Threat Model & Mitigations

### Threats & Responses

| Threat | Mitigation |
|--------|-----------|
| **Brute Force Login** | Account lockout after 5 failed attempts |
| **Stolen Refresh Token** | Token rotation on use, hash storage |
| **Token Reuse Attack** | Immediate revocation of all sessions |
| **Password Crack** | Argon2id hashing with high params |
| **Session Hijacking** | HttpOnly Secure cookies, token validation |
| **Privilege Escalation** | Role-based access + claims validation |
| **Data Breach** | Encrypted storage + hashing strategy |
| **Insider Threat** | Comprehensive audit logging |
| **CSRF** | Anti-CSRF tokens on state-changing ops |
| **SQL Injection** | EF Core parameterized queries |
| **XSS** | Output encoding, CSP headers |
| **DDoS** | Rate limiting, WAF (Azure) |

---

## 🔄 Data Consistency

### Event Sourcing & Sagas

```
Distributed Transaction Example:
Create User + Create Profile + Assign Role

1. AuthService starts transaction
   - Create User entity
   - Publish UserCreatedEvent
   - Mark as "pending"

2. UserManagementService receives event
   - Create UserProfile
   - Assigns User role
   - Publishes UserProfileCreatedEvent

3. AuthService receives confirmation
   - Marks user as "active"
   - Transaction complete

If step 2 fails:
- UserManagementService publishes UserProfileCreationFailed
- AuthService receives and reverts/alerts user
```

### Idempotency

```
Messages include:
- Unique message ID
- IdempotencyKey (same request = same result)
- Timestamp
- CorrelationId (trace across services)

Database tracks:
- processed_messages table
- (service_id, message_id) unique constraint
- Prevents duplicate processing
```

---

## 🌐 Cross-Service Communication

### Synchronous (REST)
- User lookup between services
- Permission verification
- Real-time data fetching

### Asynchronous (RabbitMQ/MassTransit)
- User lifecycle events
- Audit logging
- Cache invalidation
- Non-critical notifications

### Contract Versioning

```csharp
// Events include version info
public abstract class DomainEvent 
{
    public int EventVersion { get; } = 1;
    public string EventType => GetType().FullName;
}

// Services handle multiple versions
public class UserCreatedEventConsumer : 
    IConsumer<UserCreatedEvent.V1>,
    IConsumer<UserCreatedEvent.V2>
{
    // Supports both old and new formats
}
```

---

## 📈 Performance Considerations

### Caching Strategy

```
Redis Keys:
- user:{userId} -> 1 hour TTL
  Cached data: ID, email, roles, permissions

- refresh_token_blacklist:{tokenId} -> 7 days
  Track revoked/rotated tokens

- session_count:{userId} -> 1 hour
  Active session counter
```

### Database Optimization

```sql
-- Frequently used queries indexed
CREATE INDEX idx_user_email ON users(email) WHERE NOT deleted;
CREATE INDEX idx_refresh_token_active 
  ON refresh_tokens(user_id, revoked_at) 
  WHERE revoked_at IS NULL;

-- Partition audit logs by date
CREATE TABLE audit_logs_2026_04 PARTITION OF audit_logs
  FOR VALUES FROM ('2026-04-01') TO ('2026-05-01');
```

---

## 🚀 Scalability Design

### Horizontal Scaling

**Stateless Services**
- AuthService can run multiple instances
- No session affinity needed
- Load balancer distributes requests

**Distributed Cache**
- Redis cluster for token blacklist
- Ensures consistency across instances

**Message Queue**
- RabbitMQ handles concurrent events
- Scales independently of services
- Automatic retries on failure

---

## 🔍 Monitoring & Logging

### Key Metrics to Track

```
Security:
- Failed login attempts per user
- Token reuse attempts
- Role permission denials
- Unusual login patterns

Performance:
- Login response time
- Token refresh latency
- Database query times
- Message queue depth

Availability:
- Service uptime
- Database availability
- Message queue health
```

### Structured Logging

```csharp
Log.Information("User login successful, UserId: {@UserId}, IPAddress: {IPAddress}", 
    userId, ipAddress);

Log.Warning("Failed login attempt, Email: {Email}, Attempts: {AttemptCount}", 
    email, attemptCount);

Log.Error("Token validation failed, Error: {ErrorCode}", errorCode);
```

---

**Architecture Version**: 1.0  
**Last Updated**: 4 kwietnia 2026  
**Security Review**: Pending Implementation Phase
