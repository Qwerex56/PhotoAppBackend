namespace UserManagementService.Application.Commands.UserManagement;

using Shared.Contracts.Commands;
using Shared.Results;

/// <summary>
/// Updates the user profile data.
/// </summary>
public sealed class UpdateProfileCommand : Command<Result<UpdateProfileCommandResponse>>
{
    public Guid UserId { get; init; }
    public string? FullName { get; init; }
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
}

/// <summary>
/// Response returned after a successful profile update.
/// </summary>
public sealed class UpdateProfileCommandResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Assigns a role to a user profile.
/// </summary>
public sealed class AssignRoleCommand : Command<Result<AssignRoleCommandResponse>>
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Response returned after a successful role assignment.
/// </summary>
public sealed class AssignRoleCommandResponse
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public DateTime AssignedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Removes a role from a user profile.
/// </summary>
public sealed class RemoveRoleCommand : Command<Result>
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
}