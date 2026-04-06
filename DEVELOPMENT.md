# Development Guide - PhotoApp Backend

## 🎯 Implementation Roadmap

### Phase 1: Core Services (Current)
Establish AuthService and UserManagementService with basic functionality.

#### AuthService - Immediate Tasks
1. **Implement Application Services** (`AuthService.Application/Services`)
   - `PasswordHasher` - Argon2id hashing with OWASP parameters
   - `TokenService` - JWT generation/validation
   - `EmailTokenService` - Secure token generation and hashing
   - `PasswordValidator` - Enforce security requirements
   - `EmailService` - Send verification/reset emails

2. **Complete Command Handlers** (`AuthService.Application/Handlers`)
   - Fill in business logic for all 7 handlers
   - Implement transaction management
   - Add MassTransit event publishing
   - Handle error scenarios

3. **Create API Layer** (`AuthService.API`)
   - Program.cs - Dependency injection setup
   - Middleware configuration
   - Minimal API endpoints:
     - POST /api/auth/register
     - POST /api/auth/login
     - POST /api/auth/refresh
     - POST /api/auth/verify-email
     - POST /api/auth/request-password-reset
     - POST /api/auth/reset-password
     - POST /api/auth/logout

4. **Database Migrations**
   - Create initial migration
   - Script for seeding system data

#### UserManagementService - Immediate Tasks
1. **Event Consumers**
   - Subscribe to `UserRegisteredEvent` from AuthService
   - Auto-create UserProfile when user registers
   - Subscribe to `UserDeletedEvent` for cleanup

2. **Command Handlers**
   - UpdateProfileCommand
   - AssignRoleCommand
   - RemoveRoleCommand

3. **API Endpoints**
   - GET /api/users/{id}/profile
   - PUT /api/users/{id}/profile
   - GET /api/users/{id}/roles
   - POST/DELETE /api/users/{id}/roles/{roleId}

4. **Database Migrations**
   - Create initial migration
   - Seed default roles (Admin, User, Moderator)

### Phase 2: Integration & Security
Connect services, implement security policies, add MFA support.

### Phase 3: Media Service
Photo upload, storage, and sharing functionality.

---

## 💡 Implementation Patterns

### Adding a New Feature

1. **Define Contract** (Shared.Contracts)
   ```csharp
   // Define event or command
   public class UserProfileUpdatedEvent : DomainEvent { ... }
   ```

2. **Create Domain Entity** (Service.Domain)
   ```csharp
   // No business logic in model
   public class UserProfile { ... }
   ```

3. **Define Repository Interface** (Service.Domain/Repositories)
   ```csharp
   public interface IUserProfileRepository { ... }
   ```

4. **Implement Repository** (Service.Infrastructure/Repositories)
   ```csharp
   public class UserProfileRepository : IUserProfileRepository { ... }
   ```

5. **Create Command** (Service.Application/Commands)
   ```csharp
   public class UpdateProfileCommand : Command<Result> { ... }
   ```

6. **Create Validator** (Service.Application/Validators)
   ```csharp
   public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand> { ... }
   ```

7. **Create Handler** (Service.Application/Handlers)
   ```csharp
   public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result> { ... }
   ```

8. **Create API Endpoint** (Service.API)
   ```csharp
   app.MapPut("/api/users/{id}/profile", UpdateProfileEndpoint);
   ```

---

## 🛠️ Practical Implementation Steps

### Setting Up AuthService.Infrastructure Services

```csharp
// Create new file: AuthService.Infrastructure/Services/PasswordHasher.cs
public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        var argon2id = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            DegreeOfParallelism = AuthConstants.PasswordHashParallelism,
            MemorySize = AuthConstants.PasswordHashMemorySize,
            Iterations = AuthConstants.PasswordHashIterations
        };
        
        return Convert.ToBase64String(argon2id.GetBytes(32));
    }

    public bool VerifyPassword(string password, string hash)
    {
        var argon2id = new Argon2id(Encoding.UTF8.GetBytes(password));
        return argon2id.Verify(Convert.FromBase64String(hash));
    }
}
```

### Setting Up MassTransit in Program.cs

```csharp
// Add to AuthService.API/Program.cs
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserCreatedEventConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", h =>
        {
            h.Username("photoapp_user");
            h.Password("photoapp_secure_password_123!");
        });
        
        cfg.ConfigureEndpoints(context);
    });
});
```

### Dependency Injection Setup

```csharp
// Add to AuthService.API/Program.cs
// Domain layer - repositories
builder.Services.AddScoped<IAuthUnitOfWork, AuthUnitOfWork>();

// Application layer - services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Handlers & validation
builder.Services.AddMediatR(typeof(AuthService.Application.AssemblyReference));
builder.Services.AddValidatorsFromAssembly(typeof(RegisterCommandValidator).Assembly);
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Database
builder.Services.AddDbContext<AuthServiceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## 🔐 Security Checklist

- [ ] All passwords hashed with Argon2id
- [ ] Tokens stored as hashes (never plain text)
- [ ] Token rotation implemented
- [ ] Token reuse detection implemented
- [ ] Account lockout after N failed attempts
- [ ] Email verification required
- [ ] Password reset tokens single-use
- [ ] All timestamps in UTC
- [ ] HTTPS enforced in production
- [ ] CORS configured properly
- [ ] Rate limiting implemented
- [ ] SQL injection prevented (EF Core parameterized)
- [ ] Audit logging for security events
- [ ] Sensitive data not logged
- [ ] Error messages don't leak information

---

## 📝 Code Review Checklist

Before marking a feature as complete:

- [ ] All methods are async
- [ ] Results are properly handled (no unchecked exceptions)
- [ ] Validators are used on all commands
- [ ] Database operations use Unit of Work
- [ ] Events are published for cross-service communication
- [ ] Error codes use constants (not magic strings)
- [ ] DTOs used for API contracts
- [ ] Timestamps are UTC
- [ ] Unit tests exist
- [ ] No TODO comments left

---

## 🧪 Testing Strategy

### Unit Tests
- Service logic (handlers, validators)
- Password hashing & token generation
- Entity state transitions

### Integration Tests
- Database operations
- MassTransit event publishing
- Full command workflow

### API Tests
- Endpoint authorization
- Request validation
- Response formats

---

## 📋 Database Seed Data

Default roles to create during migration:

```sql
INSERT INTO roles (id, name, description, is_system, created_at, updated_at) VALUES
('00000000-0000-0000-0000-000000000001', 'Admin', 'Administrator with full system access', true, NOW(), NOW()),
('00000000-0000-0000-0000-000000000002', 'User', 'Regular user with standard permissions', true, NOW(), NOW()),
('00000000-0000-0000-0000-000000000003', 'Moderator', 'Moderator with content management', true, NOW(), NOW());
```

---

## 🐛 Debugging Tips

### Common Issues

1. **Migrations not found**
   ```bash
   dotnet ef migrations list --project src/Services/AuthService/AuthService.Infrastructure \
     --startup-project src/Services/AuthService/AuthService.API
   ```

2. **Database connection failed**
   - Check PostgreSQL is running: `docker-compose ps`
   - Verify credentials in appsettings.json
   - Test connection: `docker exec photoapp_postgres psql -U photoapp_user -d authservice -c "SELECT 1"`

3. **RabbitMQ not responding**
   - Check RabbitMQ container: `docker exec photoapp_rabbitmq rabbitmq-diagnostics ping`
   - View logs: `docker logs photoapp_rabbitmq`

4. **MassTransit not consuming events**
   - Verify consumers are registered
   - Check RabbitMQ management UI: http://localhost:15672
   - Enable debug logging for MassTransit

---

## 📚 Useful Commands

```bash
# Create new migration
dotnet ef migrations add MigrationName \
  --project src/Services/AuthService/AuthService.Infrastructure \
  --startup-project src/Services/AuthService/AuthService.API \
  --output-dir Persistence/Migrations

# Update database
dotnet ef database update \
  --project src/Services/AuthService/AuthService.Infrastructure \
  --startup-project src/Services/AuthService/AuthService.API

# View migration script (SQL)
dotnet ef migrations script LastMigration \
  --project src/Services/AuthService/AuthService.Infrastructure \
  --startup-project src/Services/AuthService/AuthService.API

# Connect to database
docker exec -it photoapp_postgres psql -U photoapp_user -d authservice

# View Docker logs
docker-compose logs -f [service_name]

# Restart services
docker-compose restart
```

---

## 🤝 Contributing Guidelines

1. Create feature branch: `git checkout -b feature/add-mfa-support`
2. Implement feature following patterns in this guide
3. Add unit tests
4. Self-review code against checklist
5. Create pull request with detailed description
6. Address review feedback
7. Merge to main

---

**Next Review Point**: After AuthService MVP completion
