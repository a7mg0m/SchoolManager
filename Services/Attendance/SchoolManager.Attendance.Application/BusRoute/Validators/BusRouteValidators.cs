using FluentValidation;

namespace SchoolManager.Attendance.Application
{
    public class CreateBusRouteCommandValidator : AbstractValidator<CreateBusRouteCommand>
    {
        public CreateBusRouteCommandValidator()
        {
            RuleFor(x => x.NameAr).NotEmpty().WithMessage("NameArRequired");
            RuleFor(x => x.NameEn).NotEmpty().WithMessage("NameEnRequired");
            RuleFor(x => x.Description).MaximumLength(Domain.BusRoute.MaxDescriptionLength).WithMessage("DescriptionTooLong");
            RuleForEach(x => x.Stops).SetValidator(new BusRouteStopDtoValidator());
            RuleFor(x => x.Stops).Must(BusRouteStopDtoValidator.HaveUniqueOrders).WithMessage("StopOrderDuplicated");
        }
    }

    public class UpdateBusRouteCommandValidator : AbstractValidator<UpdateBusRouteCommand>
    {
        public UpdateBusRouteCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.NameAr).NotEmpty().WithMessage("NameArRequired");
            RuleFor(x => x.NameEn).NotEmpty().WithMessage("NameEnRequired");
            RuleFor(x => x.Description).MaximumLength(Domain.BusRoute.MaxDescriptionLength).WithMessage("DescriptionTooLong");
            RuleForEach(x => x.Stops).SetValidator(new BusRouteStopDtoValidator());
            RuleFor(x => x.Stops).Must(BusRouteStopDtoValidator.HaveUniqueOrders).WithMessage("StopOrderDuplicated");
        }
    }

    public class BusRouteStopDtoValidator : AbstractValidator<BusRouteStopDto>
    {
        public BusRouteStopDtoValidator()
        {
            RuleFor(x => x.NameAr).NotEmpty().WithMessage("StopNameArRequired");
            RuleFor(x => x.NameEn).NotEmpty().WithMessage("StopNameEnRequired");
            RuleFor(x => x.Order).GreaterThan(0).WithMessage("StopOrderInvalid");
        }

        public static bool HaveUniqueOrders(List<BusRouteStopDto>? stops)
            => stops is null || stops.Select(s => s.Order).Distinct().Count() == stops.Count;
    }

    public class DeleteBusRouteCommandValidator : AbstractValidator<DeleteBusRouteCommand>
    {
        public DeleteBusRouteCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
        }
    }
}
