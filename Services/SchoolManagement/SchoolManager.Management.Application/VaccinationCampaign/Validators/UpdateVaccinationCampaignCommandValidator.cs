using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class UpdateVaccinationCampaignCommandValidator : AbstractValidator<UpdateVaccinationCampaignCommand>
    {
        public UpdateVaccinationCampaignCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("IdRequired");
            RuleFor(x => x.VaccineId).NotEmpty().WithMessage("VaccineIdRequired");
            RuleFor(x => x.Title).NotEmpty().WithMessage("TitleRequired");
            RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).WithMessage("EndDateBeforeStartDate");
        }
    }
}
