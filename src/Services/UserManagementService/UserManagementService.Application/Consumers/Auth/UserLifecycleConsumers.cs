namespace UserManagementService.Application.Consumers.Auth;

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events.Auth;
using Shared.Contracts.Events.Users;
using UserManagementService.Domain.Entities;
using UserManagementService.Domain.Repositories;

/// <summary>
/// Creates a user profile when the auth service publishes a registration event.
/// </summary>
public sealed class UserRegisteredEventConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly IUserManagementUnitOfWork _unitOfWork;

    public UserRegisteredEventConsumer(IUserManagementUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var profile = await _unitOfWork.UserProfiles.GetByIdAsync(context.Message.UserId, context.CancellationToken)
            ?? await _unitOfWork.UserProfiles.GetByEmailAsync(context.Message.Email, context.CancellationToken);

        if (profile is not null)
        {
            return;
        }

        var userProfile = new UserProfile(context.Message.UserId, context.Message.Email, context.Message.FullName);

        try
        {
            await _unitOfWork.UserProfiles.CreateAsync(userProfile, context.CancellationToken);
        }
        catch (DbUpdateException)
        {
            throw;
        }
    }
}

/// <summary>
/// Removes profile data when the auth service publishes a user deletion event.
/// </summary>
public sealed class UserDeletedEventConsumer : IConsumer<UserDeletedEvent>
{
    private readonly IUserManagementUnitOfWork _unitOfWork;

    public UserDeletedEventConsumer(IUserManagementUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
        var profile = await _unitOfWork.UserProfiles.GetByIdAsync(context.Message.UserId, context.CancellationToken);

        if (profile is null)
        {
            return;
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(context.CancellationToken);

            await _unitOfWork.UserRoles.RemoveAllRolesAsync(profile.Id, context.CancellationToken);
            await _unitOfWork.UserProfiles.DeleteAsync(profile.Id, context.CancellationToken);

            await _unitOfWork.CommitTransactionAsync(context.CancellationToken);
        }
        catch (DbUpdateException)
        {
            await _unitOfWork.RollbackTransactionAsync(context.CancellationToken);
            throw;
        }
    }
}