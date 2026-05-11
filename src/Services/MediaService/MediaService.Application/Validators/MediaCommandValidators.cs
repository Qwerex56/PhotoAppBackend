namespace MediaService.Application.Validators;

using FluentValidation;
using MediaService.Application.Commands.Media;

public sealed class CreateAlbumCommandValidator : AbstractValidator<CreateAlbumCommand>
{
    public CreateAlbumCommandValidator()
    {
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class RenameAlbumCommandValidator : AbstractValidator<RenameAlbumCommand>
{
    public RenameAlbumCommandValidator()
    {
        RuleFor(x => x.AlbumId).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class DeleteAlbumCommandValidator : AbstractValidator<DeleteAlbumCommand>
{
    public DeleteAlbumCommandValidator()
    {
        RuleFor(x => x.AlbumId).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(250);
    }
}

public sealed class ShareAlbumCommandValidator : AbstractValidator<ShareAlbumCommand>
{
    public ShareAlbumCommandValidator()
    {
        RuleFor(x => x.AlbumId).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.SharedWithUserId).NotEmpty();
        RuleFor(x => x.Permission).NotEmpty().Must(value => value is "view" or "edit");
        RuleFor(x => x.ExpiresAt).Must(value => value is null || value > DateTime.UtcNow);
    }
}

public sealed class UploadMediaCommandValidator : AbstractValidator<UploadMediaCommand>
{
    public UploadMediaCommandValidator()
    {
        RuleFor(x => x.AlbumId).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.OriginalFileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FileSize).GreaterThan(0);
        RuleFor(x => x.Content).NotNull();
    }
}

public sealed class RenameMediaCommandValidator : AbstractValidator<RenameMediaCommand>
{
    public RenameMediaCommandValidator()
    {
        RuleFor(x => x.MediaId).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(150);
    }
}

public sealed class MoveMediaCommandValidator : AbstractValidator<MoveMediaCommand>
{
    public MoveMediaCommandValidator()
    {
        RuleFor(x => x.MediaId).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.TargetAlbumId).NotEmpty();
    }
}

public sealed class DeleteMediaCommandValidator : AbstractValidator<DeleteMediaCommand>
{
    public DeleteMediaCommandValidator()
    {
        RuleFor(x => x.MediaId).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(250);
    }
}

public sealed class ShareMediaCommandValidator : AbstractValidator<ShareMediaCommand>
{
    public ShareMediaCommandValidator()
    {
        RuleFor(x => x.MediaId).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.SharedWithUserId).NotEmpty();
        RuleFor(x => x.Permission).NotEmpty().Must(value => value is "view" or "edit");
        RuleFor(x => x.ExpiresAt).Must(value => value is null || value > DateTime.UtcNow);
    }
}

public sealed class AddMediaTagCommandValidator : AbstractValidator<AddMediaTagCommand>
{
    public AddMediaTagCommandValidator()
    {
        RuleFor(x => x.MediaId).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.TagName).NotEmpty().MaximumLength(64);
    }
}

public sealed class RemoveMediaTagCommandValidator : AbstractValidator<RemoveMediaTagCommand>
{
    public RemoveMediaTagCommandValidator()
    {
        RuleFor(x => x.MediaId).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.TagName).NotEmpty().MaximumLength(64);
    }
}