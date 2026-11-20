using FluentValidation;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    public class CreateTripCommandValidator : AbstractValidator<CreateTripCommand>
    {
        public CreateTripCommandValidator()
        {
            RuleFor(x => x.BusId).NotEmpty().WithMessage("BusIdRequired");
            RuleFor(x => x.Date).NotEmpty().WithMessage("DateRequired");
            RuleFor(x => x.Type).IsInEnum().WithMessage("TripTypeInvalid");
        }
    }

    public class StartTripCommandValidator : AbstractValidator<StartTripCommand>
    {
        public StartTripCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }

    public class CompleteTripCommandValidator : AbstractValidator<CompleteTripCommand>
    {
        public CompleteTripCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }

    public class CancelTripCommandValidator : AbstractValidator<CancelTripCommand>
    {
        public CancelTripCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }

    public class UpdateTripLocationCommandValidator : AbstractValidator<UpdateTripLocationCommand>
    {
        public UpdateTripLocationCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).WithMessage("LatitudeInvalid");
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).WithMessage("LongitudeInvalid");
        }
    }

    public class MarkTripStudentCommandValidator : AbstractValidator<MarkTripStudentCommand>
    {
        public MarkTripStudentCommandValidator()
        {
            RuleFor(x => x.TripId).NotEmpty().WithMessage("TripIdRequired");
            RuleFor(x => x.StudentId).NotEmpty().WithMessage("StudentIdRequired");
            RuleFor(x => x.Status).IsInEnum().WithMessage("StatusInvalid")
                .NotEqual(TripStudentStatus.Pending).WithMessage("StatusInvalid");
        }
    }
}
