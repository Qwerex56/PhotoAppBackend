namespace UserManagementService.Application.Validators;

using FluentValidation;
using UserManagementService.Application.Commands.UserManagement;

/// <summary>
/// Validates profile update requests.
/// </summary>
public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x)
            .Must(x => x.FullName is not null || x.Bio is not null || x.AvatarUrl is not null)
            .WithMessage("At least one profile field must be provided");

        RuleFor(x => x.FullName)
            .MaximumLength(255).WithMessage("Full name cannot exceed 255 characters")
            .Must(value => value is null || !string.IsNullOrWhiteSpace(value))
            .WithMessage("Full name cannot be empty");

        RuleFor(x => x.Bio)
            .MaximumLength(1000).WithMessage("Bio cannot exceed 1000 characters");

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500).WithMessage("Avatar URL cannot exceed 500 characters");
    }
}

/// <summary>
/// Validates role assignment requests.
/// </summary>
public sealed class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required");

        RuleFor(x => x.ExpiresAt)
            .Must(value => value is null || value > DateTime.UtcNow)
            .WithMessage("Expiration date must be in the future");
    }
}

/// <summary>
/// Validates role removal requests.
/// </summary>
public sealed class RemoveRoleCommandValidator : AbstractValidator<RemoveRoleCommand>
{
    public RemoveRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required");
    }
}