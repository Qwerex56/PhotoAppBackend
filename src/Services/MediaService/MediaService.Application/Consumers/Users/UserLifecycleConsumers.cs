namespace MediaService.Application.Consumers.Users;

using MassTransit;
using Shared.Contracts.Events.Users;
using MediaService.Domain.Repositories;

public sealed class UserDeletedEventConsumer : IConsumer<UserDeletedEvent>
{
    private readonly IMediaUnitOfWork _unitOfWork;

    public UserDeletedEventConsumer(IMediaUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
        await _unitOfWork.BeginTransactionAsync(context.CancellationToken);

        try
        {
            var albums = await _unitOfWork.Albums.GetOwnedByUserAsync(context.Message.UserId, context.CancellationToken);

            foreach (var album in albums)
            {
                album.Delete();
                await _unitOfWork.Albums.UpdateAsync(album, context.CancellationToken);
                await _unitOfWork.AlbumShares.RevokeAllForAlbumAsync(album.Id, context.CancellationToken);
            }

            await _unitOfWork.MediaAssets.DeleteByOwnerAsync(context.Message.UserId, context.CancellationToken);

            await _unitOfWork.CommitTransactionAsync(context.CancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(context.CancellationToken);
            throw;
        }
    }
}