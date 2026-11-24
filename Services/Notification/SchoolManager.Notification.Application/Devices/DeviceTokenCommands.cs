using BuildingBlocks;
using BuildingBlocks.CQRS;
using FluentValidation;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>
    /// The app's Firebase token for the logged-in user (after login / on app start / when Firebase renews it).
    /// Upsert by token: a token of another user is reassigned (another user signed in on the device).
    /// The user and their kind come from the login token, never from the request.
    /// </summary>
    public record RegisterDeviceTokenCommand(string Token, DevicePlatform Platform, string AppName) : ICommand<bool>;

    /// <summary>Removes the token at logout; only a token of the logged-in user is removed (false otherwise)</summary>
    public record UnregisterDeviceTokenCommand(string Token) : ICommand<bool>;

    public class RegisterDeviceTokenCommandValidator : AbstractValidator<RegisterDeviceTokenCommand>
    {
        public RegisterDeviceTokenCommandValidator()
        {
            RuleFor(x => x.Token).NotEmpty().WithMessage("TokenRequired")
                .MaximumLength(UserDeviceToken.TokenMaxLength).WithMessage("TokenTooLong");
            RuleFor(x => x.Platform).IsInEnum().WithMessage("PlatformInvalid");
            RuleFor(x => x.AppName).NotEmpty().WithMessage("AppNameRequired")
                .Must(a => UserDeviceToken.AppNames.Contains((a ?? string.Empty).Trim().ToLowerInvariant()))
                .WithMessage("AppNameInvalid");
        }
    }

    public class UnregisterDeviceTokenCommandValidator : AbstractValidator<UnregisterDeviceTokenCommand>
    {
        public UnregisterDeviceTokenCommandValidator()
        {
            RuleFor(x => x.Token).NotEmpty().WithMessage("TokenRequired")
                .MaximumLength(UserDeviceToken.TokenMaxLength).WithMessage("TokenTooLong");
        }
    }

    public class RegisterDeviceTokenCommandHandler(
        ICurrentUserService currentUser,
        IUserDeviceTokenRepository repository,
        IUnitOfWork unitOfWork)
        : ICommandHandler<RegisterDeviceTokenCommand, bool>
    {
        public async Task<bool> Handle(RegisterDeviceTokenCommand command, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId;
            if (userId == Guid.Empty)
                throw new UnauthorizedAccessException("Sign in to register a device.");

            var userType = DeviceUserTypes.Of(currentUser);
            var token = command.Token.Trim();
            var audit = new AuditInfo(DateTime.Now, userId);

            var existing = await repository.GetByTokenAsync(token, cancellationToken);
            if (existing is null)
            {
                await repository.AddAsync(new UserDeviceToken(userId, userType, token, command.Platform, command.AppName, audit));
            }
            else if (existing.UserId != userId || existing.UserType != userType)
            {
                existing.Reassign(userId, userType, existing.AuditInfo.Updated(DateTime.Now, userId));
                existing.Touch(command.Platform, command.AppName);
            }
            else
            {
                existing.Touch(command.Platform, command.AppName);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public class UnregisterDeviceTokenCommandHandler(
        ICurrentUserService currentUser,
        IUserDeviceTokenRepository repository,
        IUnitOfWork unitOfWork)
        : ICommandHandler<UnregisterDeviceTokenCommand, bool>
    {
        public async Task<bool> Handle(UnregisterDeviceTokenCommand command, CancellationToken cancellationToken)
        {
            var existing = await repository.GetByTokenAsync(command.Token.Trim(), cancellationToken);
            if (existing is null || existing.UserId != currentUser.UserId)
                return false;

            repository.Remove(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    /// <summary>The kind of the logged-in user, from the login token's claims</summary>
    public static class DeviceUserTypes
    {
        public static DeviceUserType Of(ICurrentUserService user) =>
            user.ParentId is not null ? DeviceUserType.Parent
            : user.TeacherId is not null ? DeviceUserType.Teacher
            : user.BusId is not null ? DeviceUserType.Driver
            : DeviceUserType.Employee;
    }
}
