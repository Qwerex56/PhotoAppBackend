namespace UserManagementService.API.Endpoints;

using System.Security.Claims;
using MediatR;
using Shared.Constants;
using Shared.Results;
using UserManagementService.Application.Commands.UserManagement;
using UserManagementService.Domain.Repositories;

public static class UserManagementEndpoints
{
    public static IEndpointRouteBuilder MapUserManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("UserManagement")
            .RequireAuthorization();

        group.MapGet("/{userId:guid}/profile", GetProfileAsync)
            .WithName("GetUserProfile")
            .WithOpenApi();

        group.MapGet("/by-email", GetByEmailAsync)
            .WithName("GetUserByEmail")
            .WithOpenApi();

        group.MapPut("/{userId:guid}/profile", UpdateProfileAsync)
            .WithName("UpdateUserProfile")
            .WithOpenApi();

        group.MapGet("/{userId:guid}/roles", GetRolesAsync)
            .WithName("GetUserRoles")
            .WithOpenApi();

        group.MapPost("/{userId:guid}/roles/{roleId:guid}", AssignRoleAsync)
            .WithName("AssignUserRole")
            .WithOpenApi();

        group.MapDelete("/{userId:guid}/roles/{roleId:guid}", RemoveRoleAsync)
            .WithName("RemoveUserRole")
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetProfileAsync(Guid userId, IUserManagementUnitOfWork unitOfWork)
    {
        var profile = await unitOfWork.UserProfiles.GetByIdAsync(userId);

        if (profile is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new UserProfileResponse(
            profile.Id,
            profile.Email,
            profile.FullName,
            profile.Bio,
            profile.AvatarUrl,
            profile.IsDeleted,
            profile.CreatedAt,
            profile.UpdatedAt,
            profile.DeletedAt));
    }

    private static async Task<IResult> GetByEmailAsync(string email, IUserManagementUnitOfWork unitOfWork)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Results.BadRequest();

        var profile = await unitOfWork.UserProfiles.GetByEmailAsync(email);

        if (profile is null)
            return Results.NotFound();

        return Results.Ok(new UserProfileResponse(
            profile.Id,
            profile.Email,
            profile.FullName,
            profile.Bio,
            profile.AvatarUrl,
            profile.IsDeleted,
            profile.CreatedAt,
            profile.UpdatedAt,
            profile.DeletedAt));
    }

    private static async Task<IResult> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        ISender sender)
    {
        var command = new UpdateProfileCommand
        {
            UserId = userId,
            FullName = request.FullName,
            Bio = request.Bio,
            AvatarUrl = request.AvatarUrl
        };

        return (await sender.Send(command)).ToHttpResult();
    }

    private static async Task<IResult> GetRolesAsync(Guid userId, IUserManagementUnitOfWork unitOfWork)
    {
        var profile = await unitOfWork.UserProfiles.GetByIdAsync(userId);

        if (profile is null)
        {
            return Results.NotFound();
        }

        var roles = await unitOfWork.UserRoles.GetUserRolesAsync(userId);

        var response = roles.Select(userRole => new UserRoleResponse(
            userRole.RoleId,
            userRole.Role.Name,
            userRole.Role.Description,
            userRole.AssignedAt,
            userRole.ExpiresAt,
            userRole.IsActive));

        return Results.Ok(response);
    }

    private static async Task<IResult> AssignRoleAsync(
        Guid userId,
        Guid roleId,
        AssignRoleRequest request,
        ISender sender)
    {
        var command = new AssignRoleCommand
        {
            UserId = userId,
            RoleId = roleId,
            ExpiresAt = request.ExpiresAt
        };

        return (await sender.Send(command)).ToHttpResult();
    }

    private static async Task<IResult> RemoveRoleAsync(Guid userId, Guid roleId, ISender sender)
    {
        var command = new RemoveRoleCommand
        {
            UserId = userId,
            RoleId = roleId
        };

        return (await sender.Send(command)).ToHttpResult();
    }

    private static IResult ToHttpResult(this Result result)
        => result.IsSuccess ? Results.NoContent() : ToProblem(result.Error!);

    private static IResult ToHttpResult<T>(this Result<T> result)
        => result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.Error!);

    private static IResult ToProblem(Error error)
    {
        var statusCode = error.Code switch
        {
            ErrorCodes.UserProfileNotFound => StatusCodes.Status404NotFound,
            ErrorCodes.InvalidRole => StatusCodes.Status404NotFound,
            ErrorCodes.RoleAlreadyAssigned => StatusCodes.Status409Conflict,
            ErrorCodes.CannotRemoveAdminRole => StatusCodes.Status403Forbidden,
            ErrorCodes.ValidationFailed => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: statusCode);
    }

    private sealed record UpdateProfileRequest(string? FullName, string? Bio, string? AvatarUrl);
    private sealed record AssignRoleRequest(DateTime? ExpiresAt);
    private sealed record UserProfileResponse(
        Guid UserId,
        string Email,
        string? FullName,
        string? Bio,
        string? AvatarUrl,
        bool IsDeleted,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? DeletedAt);
    private sealed record UserRoleResponse(
        Guid RoleId,
        string RoleName,
        string? Description,
        DateTime AssignedAt,
        DateTime? ExpiresAt,
        bool IsActive);
}