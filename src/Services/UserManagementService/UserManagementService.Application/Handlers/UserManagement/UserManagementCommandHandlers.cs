namespace UserManagementService.Application.Handlers.UserManagement;

using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserManagementService.Application.Commands.UserManagement;
using UserManagementService.Domain.Entities;
using UserManagementService.Domain.Repositories;
using Shared.Constants;
using Shared.Contracts.Events.Users;
using Shared.Results;

/// <summary>
/// Handles user profile updates.
/// </summary>
public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<UpdateProfileCommandResponse>>
{
    private readonly IUserManagementUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public UpdateProfileCommandHandler(IUserManagementUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<UpdateProfileCommandResponse>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.UserProfiles.GetByIdAsync(request.UserId, cancellationToken);

        if (profile is null)
        {
            return Result<UpdateProfileCommandResponse>.Failure(
                Error.Create(ErrorCodes.UserProfileNotFound, "User profile not found"));
        }

        profile.Update(request.FullName, request.Bio, request.AvatarUrl);

        try
        {
            await _unitOfWork.UserProfiles.UpdateAsync(profile, cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            return Result<UpdateProfileCommandResponse>.Failure(Error.InternalError(exception.Message));
        }

        await _publishEndpoint.Publish(new UserProfileUpdatedEvent
        {
            UserId = profile.Id,
            FullName = profile.FullName,
            Avatar = profile.AvatarUrl,
            Bio = profile.Bio,
            UpdatedAt = profile.UpdatedAt,
            Source = "UserManagementService.Application"
        }, cancellationToken);

        return Result<UpdateProfileCommandResponse>.Success(new UpdateProfileCommandResponse
        {
            UserId = profile.Id,
            Email = profile.Email,
            FullName = profile.FullName,
            Bio = profile.Bio,
            AvatarUrl = profile.AvatarUrl,
            UpdatedAt = profile.UpdatedAt
        });
    }
}

/// <summary>
/// Handles role assignments for user profiles.
/// </summary>
public sealed class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result<AssignRoleCommandResponse>>
{
    private readonly IUserManagementUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public AssignRoleCommandHandler(IUserManagementUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<AssignRoleCommandResponse>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.UserProfiles.GetByIdAsync(request.UserId, cancellationToken);

        if (profile is null)
        {
            return Result<AssignRoleCommandResponse>.Failure(
                Error.Create(ErrorCodes.UserProfileNotFound, "User profile not found"));
        }

        var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
        {
            return Result<AssignRoleCommandResponse>.Failure(
                Error.Create(ErrorCodes.InvalidRole, "Role not found"));
        }

        var hasRole = await _unitOfWork.UserRoles.UserHasRoleAsync(request.UserId, request.RoleId, cancellationToken);

        if (hasRole)
        {
            return Result<AssignRoleCommandResponse>.Failure(
                Error.Create(ErrorCodes.RoleAlreadyAssigned, $"User already has role '{role.Name}'"));
        }

        var userRole = new UserRole(request.UserId, request.RoleId, request.ExpiresAt);

        try
        {
            await _unitOfWork.UserRoles.AssignRoleAsync(userRole, cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            return Result<AssignRoleCommandResponse>.Failure(Error.InternalError(exception.Message));
        }

        await _publishEndpoint.Publish(new UserRoleChangedEvent
        {
            UserId = profile.Id,
            RoleName = role.Name,
            Assigned = true,
            ChangedAt = userRole.AssignedAt,
            Source = "UserManagementService.Application"
        }, cancellationToken);

        return Result<AssignRoleCommandResponse>.Success(new AssignRoleCommandResponse
        {
            UserId = profile.Id,
            RoleId = role.Id,
            RoleName = role.Name,
            AssignedAt = userRole.AssignedAt,
            ExpiresAt = userRole.ExpiresAt
        });
    }
}

/// <summary>
/// Handles role removals for user profiles.
/// </summary>
public sealed class RemoveRoleCommandHandler : IRequestHandler<RemoveRoleCommand, Result>
{
    private readonly IUserManagementUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public RemoveRoleCommandHandler(IUserManagementUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result> Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.UserProfiles.GetByIdAsync(request.UserId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(Error.Create(ErrorCodes.UserProfileNotFound, "User profile not found"));
        }

        var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure(Error.Create(ErrorCodes.InvalidRole, "Role not found"));
        }

        if (role.Name == RoleNames.Admin)
        {
            return Result.Failure(Error.Create(ErrorCodes.CannotRemoveAdminRole, "Admin role cannot be removed"));
        }

        var hasRole = await _unitOfWork.UserRoles.UserHasRoleAsync(request.UserId, request.RoleId, cancellationToken);

        if (!hasRole)
        {
            return Result.Success();
        }

        try
        {
            await _unitOfWork.UserRoles.RemoveRoleAsync(request.UserId, request.RoleId, cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            return Result.Failure(Error.InternalError(exception.Message));
        }

        await _publishEndpoint.Publish(new UserRoleChangedEvent
        {
            UserId = profile.Id,
            RoleName = role.Name,
            Assigned = false,
            ChangedAt = DateTime.UtcNow,
            Source = "UserManagementService.Application"
        }, cancellationToken);

        return Result.Success();
    }
}