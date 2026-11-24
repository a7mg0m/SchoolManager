using FluentValidation;

namespace SchoolManager.Attendance.Application
{
    public class CreateBusCommandValidator : AbstractValidator<CreateBusCommand>
    {
        public CreateBusCommandValidator()
        {
            RuleFor(x => x.Number).NotEmpty().WithMessage("NumberRequired").MaximumLength(50).WithMessage("NumberTooLong");
            RuleFor(x => x.PlateNumber).NotEmpty().WithMessage("PlateNumberRequired").MaximumLength(50).WithMessage("PlateNumberTooLong");
            RuleFor(x => x.Capacity).GreaterThan(0).WithMessage("CapacityMustBeGreaterThanZero");
            RuleFor(x => x.DriverName).NotEmpty().WithMessage("DriverNameRequired").MaximumLength(200).WithMessage("DriverNameTooLong");
            RuleFor(x => x.DriverPhone).NotEmpty().WithMessage("DriverPhoneRequired").MaximumLength(50).WithMessage("DriverPhoneTooLong");
            RuleFor(x => x.BusRouteId).NotEmpty().WithMessage("BusRouteIdRequired");

            // Driver's login: both required for a new bus
            RuleFor(x => x.DriverLoginName)
                .NotEmpty().WithMessage("LoginNameRequired")
                .MinimumLength(BusDriverAccount.LoginNameMinLength).WithMessage("LoginNameTooShort")
                .MaximumLength(BusDriverAccount.LoginNameMaxLength).WithMessage("LoginNameTooLong");
            RuleFor(x => x.DriverPassword)
                .NotEmpty().WithMessage("PasswordRequired")
                .MinimumLength(BusDriverAccount.PasswordMinLength).WithMessage("PasswordTooShort");
        }
    }

    public class UpdateBusCommandValidator : AbstractValidator<UpdateBusCommand>
    {
        public UpdateBusCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Number).NotEmpty().WithMessage("NumberRequired").MaximumLength(50).WithMessage("NumberTooLong");
            RuleFor(x => x.PlateNumber).NotEmpty().WithMessage("PlateNumberRequired").MaximumLength(50).WithMessage("PlateNumberTooLong");
            RuleFor(x => x.Capacity).GreaterThan(0).WithMessage("CapacityMustBeGreaterThanZero");
            RuleFor(x => x.DriverName).NotEmpty().WithMessage("DriverNameRequired").MaximumLength(200).WithMessage("DriverNameTooLong");
            RuleFor(x => x.DriverPhone).NotEmpty().WithMessage("DriverPhoneRequired").MaximumLength(50).WithMessage("DriverPhoneTooLong");
            RuleFor(x => x.BusRouteId).NotEmpty().WithMessage("BusRouteIdRequired");

            // Driver's login: empty password keeps the current one (the handler requires it for a bus without a login yet)
            RuleFor(x => x.DriverLoginName)
                .NotEmpty().WithMessage("LoginNameRequired")
                .MinimumLength(BusDriverAccount.LoginNameMinLength).WithMessage("LoginNameTooShort")
                .MaximumLength(BusDriverAccount.LoginNameMaxLength).WithMessage("LoginNameTooLong");
            RuleFor(x => x.DriverPassword)
                .MinimumLength(BusDriverAccount.PasswordMinLength).WithMessage("PasswordTooShort")
                .When(x => !string.IsNullOrEmpty(x.DriverPassword));
        }
    }

    public class DeleteBusCommandValidator : AbstractValidator<DeleteBusCommand>
    {
        public DeleteBusCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
