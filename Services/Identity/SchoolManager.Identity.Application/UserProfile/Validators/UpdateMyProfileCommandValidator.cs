using FluentValidation;

namespace SchoolManager.Identity.Application
{
    public class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
    {
        /// <summary>Largest accepted profile image (the admin app resizes images before upload).</summary>
        public const int MaxProfileImageBytes = 1024 * 1024;

        public UpdateMyProfileCommandValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().WithMessage(ErrorKeys.FirstNameRequired).MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().WithMessage(ErrorKeys.LastNameRequired).MaximumLength(100);
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(ErrorKeys.EmailRequired)
                .EmailAddress().WithMessage(ErrorKeys.EmailInvalid)
                .MaximumLength(256);
            RuleFor(x => x.PhoneNumber).MaximumLength(50);
            RuleFor(x => x.Address).MaximumLength(500);
            RuleFor(x => x.ProfileImage)
                .Must(image => image is null || image.Length <= MaxProfileImageBytes)
                .WithMessage(ErrorKeys.ProfileImageTooLarge);
        }
    }
}
