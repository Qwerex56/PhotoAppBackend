# PhotoApp Backend - Microservices Architecture

A secure, scalable photo-sharing application built with ASP.NET Core microservices, implementing Google Photos-like functionality with enterprise-grade security practices.

## 📋 Project Overview

**Objective**: Build a distributed system for secure photo sharing with strong emphasis on:
- Data security & encryption
- Authentication & Authorization (JWT, MFA, WebAuthn)
- Microservices architecture
- Clean/Onion architecture pattern
- Async messaging with RabbitMQ
- Code-First database approach

## 🏗️ Architecture

### Microservices

1. **AuthService** - Authentication & Token Management
   - User registration & login
   - JWT access tokens (15 min TTL)
   - Refresh token rotation & revocation
   - Email verification
   - Password reset
   - MFA coordination (WebAuthn, TOTP)

2. **UserManagementService** - User Profiles & Permissions
   - User profiles (name, bio, avatar)
   - Role-based access control (RBAC)
   - User role management
   - Profile updates

3. **MediaService** (Planned)
   - Media uploads to Azure Blob Storage
   - User Delegation SAS generation
   - Metadata management
   - Sharing & permissions

4. **AuditService** (Planned)
   - Append-only audit logs
   - Security event tracking
   - Compliance reporting

## 📁 Project Structure

```
PhotoAppBackend/
├── src/
│   ├── Shared/                          # Shared libraries
│   │   ├── Shared.Contracts/            # Event & command definitions
│   │   │   ├── Events/
│   │   │   │   ├── Auth/
│   │   │   │   └── Users/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   ├── Shared.Results/              # Result<T> pattern
│   │   └── Shared.Constants/            # Global constants & error codes
│   │
│   └── Services/
│       ├── AuthService/
│       │   ├── AuthService.Domain/              # Entities, interfaces
│       │   │   ├── Entities/
│       │   │   │   ├── User.cs
│       │   │   │   ├── RefreshToken.cs
│       │   │   │   ├── EmailVerificationToken.cs
│       │   │   │   └── PasswordResetToken.cs
│       │   │   └── Repositories/
│       │   ├── AuthService.Application/         # Use cases, handlers
│       │   │   ├── Commands/
│       │   │   ├── Handlers/
│       │   │   ├── Validators/
│       │   │   └── Services/
│       │   ├── AuthService.Infrastructure/      # EF Core, repositories
│       │   │   ├── Persistence/
│       │   │   ├── Repositories/
│       │   │   └── Services/
│       │   └── AuthService.API/                 # Minimal APIs, middleware
│       │
│       └── UserManagementService/
│           ├── UserManagementService.Domain/    # Entities, interfaces
│           │   ├── Entities/
│           │   │   ├── UserProfile.cs
│           │   │   ├── Role.cs
│           │   │   └── UserRole.cs
│           │   └── Repositories/
│           ├── UserManagementService.Application/
│           │   ├── Commands/
│           │   ├── Handlers/
│           │   └── Validators/
│           ├── UserManagementService.Infrastructure/
│           │   ├── Persistence/
│           │   └── Repositories/
│           └── UserManagementService.API/
│
├── scripts/
│   └── init-databases.sql                       # PostgreSQL initialization
├── compose.yaml                                 # Docker Compose for local dev
└── PhotoAppBackend.slnx                         # Solution file

```

## 🔐 Security Implementation

### Authentication & Tokens
- **Password Hashing**: Argon2id (OWASP recommended)
  - Iterations: 4
  - Memory: 64 MB
  - Parallelism: 2
- **Access Token**: JWT with 15-minute TTL
- **Refresh Token**: Rotation strategy with hash storage
- **Token Rotation**: Automatic rotation on refresh request

### Data Protection
- Passwords: Hashed with Argon2id
- Tokens: Stored as hashes in database
- Email verification: Single-use tokens
- Password reset: Short-lived tokens with attempt limits

### Key Management
- **Azure Key Vault**: Secrets & encryption keys
- **Pepper**: Password hash pepper stored in Key Vault
- **Database**: TDE for encryption at rest

## 🗄️ Database Schema

### AuthService Database
```
users
  - id (PK)
  - email (unique)
  - password_hash
  - email_verified
  - is_locked
  - failed_login_attempts
  - ...

refresh_tokens
  - id (PK)
  - user_id (FK)
  - token_hash (unique)
  - issued_at
  - expires_at
  - revoked_at
  - rotated_to_token_id
  - ...

email_verification_tokens
  - id (PK)
  - user_id (FK)
  - email
  - token_hash (unique)
  - verified_at
  - ...

password_reset_tokens
  - id (PK)
  - user_id (FK)
  - token_hash (unique)
  - used_at
  - attempt_count
  - ...
```

### UserManagementService Database
```
user_profiles
  - id (PK)
  - email (unique)
  - full_name
  - bio
  - avatar_url
  - created_at
  - deleted_at (soft delete)

roles
  - id (PK)
  - name (unique)
  - description
  - is_system

user_roles
  - id (PK)
  - user_profile_id (FK)
  - role_id (FK)
  - assigned_at
  - expires_at
```

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Docker & Docker Compose
- PostgreSQL 16 (via Docker)
- RabbitMQ (via Docker)
- Redis (via Docker)

### Local Development Setup

1. **Start infrastructure**:
   ```bash
   docker-compose up -d
   ```

2. **Wait for services to be healthy**:
   ```bash
   # Check status
   docker-compose ps
   ```

3. **Apply migrations** (when implemented):
   ```bash
   # AuthService
   dotnet ef database update --project src/Services/AuthService/AuthService.Infrastructure \
     --startup-project src/Services/AuthService/AuthService.API

   # UserManagementService
   dotnet ef database update --project src/Services/UserManagementService/UserManagementService.Infrastructure \
     --startup-project src/Services/UserManagementService/UserManagementService.API
   ```

4. **Run services**:
   ```bash
   # Terminal 1: AuthService
   dotnet run --project src/Services/AuthService/AuthService.API

   # Terminal 2: UserManagementService
   dotnet run --project src/Services/UserManagementService/UserManagementService.API
   ```

### Docker Service Credentials

- **PostgreSQL**
  - User: `photoapp_user`
  - Password: `photoapp_secure_password_123!`
  - Port: 5432

- **RabbitMQ**
  - User: `photoapp_user`
  - Password: `photoapp_secure_password_123!`
  - AMQP Port: 5672
  - Management UI: http://localhost:15672

- **Redis**
  - Password: `photoapp_secure_password_123!`
  - Port: 6379

## 📚 Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **API** | ASP.NET Core Minimal APIs | 8.0 |
| **Orchestration** | MediatR | 12.2.0 |
| **Command Bus** | MassTransit + RabbitMQ | 8.1.2 |
| **Validation** | FluentValidation | 11.9.1 |
| **ORM** | Entity Framework Core | 8.0.3 |
| **Database** | PostgreSQL | 16 |
| **Cache/Session** | Redis | 7 |
| **Auth** | JWT Bearer | 8.0.2 |
| **Password Hashing** | Argon2id | 1.0.0 |
| **Logging** | Serilog | 3.1.1 |

## ⚙️ Configuration

### Connection Strings
Update `appsettings.Development.json` for each service:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=authservice;User Id=photoapp_user;Password=photoapp_secure_password_123!"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "photoapp_user",
    "Password": "photoapp_secure_password_123!",
    "VirtualHost": "/photoapp"
  },
  "Redis": {
    "Host": "localhost",
    "Port": 6379,
    "Password": "photoapp_secure_password_123!"
  },
  "Jwt": {
    "SecretKey": "your-secret-key-min-32-chars",
    "Issuer": "PhotoAppBackend",
    "Audience": "PhotoAppClient",
    "ExpirationMinutes": 15
  }
}
```

## 📝 Implementation Status

### ✅ Completed
- [x] Project structure & folder organization
- [x] Shared libraries (Contracts, Results, Constants)
- [x] AuthService domain entities
- [x] AuthService EF Core configuration
- [x] AuthService repositories (Unit of Work pattern)
- [x] AuthService command definitions & validators
- [x] AuthService command handler skeletons
- [x] UserManagementService domain entities
- [x] UserManagementService EF Core configuration
- [x] UserManagementService repositories
- [x] Docker Compose setup
- [x] Database initialization script
- [x] Solution file organization

### ⏳ TODO - Implementation Phase

#### AuthService
- [ ] IPasswordHasher implementation (Argon2id)
- [ ] ITokenService implementation (JWT generation/validation)
- [ ] IEmailTokenService implementation
- [ ] IPasswordValidator implementation
- [ ] Command handlers (with business logic)
- [ ] MassTransit event publishers
- [ ] API endpoints (Minimal APIs)
- [ ] Middleware (auth, error handling)
- [ ] EF Core migrations

#### UserManagementService
- [ ] Event consumers (from AuthService)
- [ ] Command handlers (create profile, manage roles)
- [ ] API endpoints
- [ ] Middleware
- [ ] EF Core migrations

#### Integration
- [ ] MassTransit RabbitMQ configuration
- [ ] Message contracts finalization
- [ ] Service-to-service communication
- [ ] Integration tests

#### Additional Services (Later)
- [ ] MediaService (uploads, SAS generation)
- [ ] AuditService (logging, compliance)
- [ ] DeviceService (device management)
- [ ] WebAuthnService (Passkey support)
- [ ] MFAService (TOTP implementation)

## 🔍 Code Patterns

### Result Pattern (Error Handling)
```csharp
// Instead of exceptions for expected failures
public async Task<Result<RegisterCommandResponse>> Handle(RegisterCommand request, ...)
{
    if (await _unitOfWork.Users.ExistsByEmailAsync(request.Email))
        return Result<RegisterCommandResponse>.Failure(
            Error.Conflict("Email already in use"));
    
    // ... success case
    return Result<RegisterCommandResponse>.Success(response);
}
```

### Command/Query Pattern (CQS)
- **Commands**: Modify state (Register, Login, ResetPassword)
- **Queries**: Read state (GetProfile, GetRoles)
- All routed through MediatR for cross-cutting concerns

### Repository Pattern (Data Access)
- Abstraction layer for data persistence
- Unit of Work coordinates multiple repositories
- Entity Framework Core as implementation detail

## 🚨 Important Notes

### TODO Markers in Code
Implementation handlers contain TODO comments marking areas that need completion:
```csharp
// TODO: Review by repo owner. Further instructions needed for COPILOT. After sending instructions made rework/implemntation HERE
```

These indicate:
1. Where business logic needs to be implemented
2. External service integrations needed (email, KMS, etc.)
3. Design decisions that need owner review

### Next Steps for Development
1. Implement concrete service classes (password hasher, token service, etc.)
2. Fill in command handler implementations
3. Create Minimal API endpoints
4. Add MassTransit event publishing
5. Write unit and integration tests
6. Deploy configuration for Azure

## 📞 Development Team Notes

- **Architecture**: Clean/Onion Architecture with Repository & Unit of Work patterns
- **Async**: All operations are async (Task-based)
- **Transactions**: Unit of Work handles transactional consistency
- **Logging**: Serilog integration at application layer
- **Validation**: FluentValidation for command validation
- **Error Handling**: Result<T> pattern for functional approach

---

**Last Updated**: 4 kwietnia 2026  
**Version**: 0.1.0 (MVP Phase)
