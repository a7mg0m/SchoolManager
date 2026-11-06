using FluentValidation;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    public class CreatePrivateChatCommandValidator : AbstractValidator<CreatePrivateChatCommand>
    {
        public CreatePrivateChatCommandValidator()
        {
            RuleFor(x => x.CurrentUserId).NotEmpty().WithMessage("CurrentUserIdRequired");
            RuleFor(x => x.OtherUserId).NotEmpty().WithMessage("OtherUserIdRequired");
            RuleFor(x => x.OtherUserId).NotEqual(x => x.CurrentUserId).WithMessage("CannotChatWithYourself");
        }
    }

    public class CreateGroupChatCommandValidator : AbstractValidator<CreateGroupChatCommand>
    {
        public CreateGroupChatCommandValidator()
        {
            RuleFor(x => x.TeacherId).NotEmpty().WithMessage("TeacherIdRequired");
            RuleFor(x => x.SectionId).NotEmpty().WithMessage("SectionIdRequired");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200).WithMessage("NameRequired");
            RuleFor(x => x.ParentIds).NotNull().WithMessage("ParentIdsRequired");
            RuleForEach(x => x.ParentIds).NotEmpty().WithMessage("ParentIdInvalid");
        }
    }

    public class AttachmentDtoValidator : AbstractValidator<AttachmentDto>
    {
        public AttachmentDtoValidator()
        {
            RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
            RuleFor(x => x.Url).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.ContentType).NotEmpty().MaximumLength(100);
            RuleFor(x => x.SizeInBytes).GreaterThanOrEqualTo(0);
        }
    }

    public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
    {
        public SendMessageCommandValidator()
        {
            RuleFor(x => x.RoomId).NotEmpty().WithMessage("RoomIdRequired");
            RuleFor(x => x.SenderId).NotEmpty().WithMessage("SenderIdRequired");
            RuleFor(x => x.Content).MaximumLength(ChatMessage.MaxContentLength).WithMessage("ContentTooLong");
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.Content) || (x.Attachments?.Count ?? 0) > 0)
                .WithMessage("MessageEmpty");
            RuleForEach(x => x.Attachments).SetValidator(new AttachmentDtoValidator());
        }
    }

    public class UpdateMessageStatusCommandValidator : AbstractValidator<UpdateMessageStatusCommand>
    {
        public UpdateMessageStatusCommandValidator()
        {
            RuleFor(x => x.RoomId).NotEmpty().WithMessage("RoomIdRequired");
            RuleFor(x => x.UserId).NotEmpty().WithMessage("UserIdRequired");
            RuleFor(x => x.MessageIds).NotEmpty().WithMessage("MessageIdsRequired");
            RuleFor(x => x.Status)
                .Must(s => s is MessageDeliveryStatus.Delivered or MessageDeliveryStatus.Seen)
                .WithMessage("StatusMustBeDeliveredOrSeen");
        }
    }

    public class GetChatListQueryValidator : AbstractValidator<GetChatListQuery>
    {
        public GetChatListQueryValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("UserIdRequired");
        }
    }

    public class GetChatHistoryQueryValidator : AbstractValidator<GetChatHistoryQuery>
    {
        public GetChatHistoryQueryValidator()
        {
            RuleFor(x => x.RoomId).NotEmpty().WithMessage("RoomIdRequired");
            RuleFor(x => x.UserId).NotEmpty().WithMessage("UserIdRequired");
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
