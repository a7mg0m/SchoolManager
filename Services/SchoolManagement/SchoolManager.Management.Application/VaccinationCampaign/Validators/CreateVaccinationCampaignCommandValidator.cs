using FluentValidation;

namespace SchoolManager.Management.Application
{
    public class CreateVaccinationCampaignCommandValidator : AbstractValidator<CreateVaccinationCampaignCommand>
    {
        public CreateVaccinationCampaignCommandValidator()
        {
            RuleFor(x => x.VaccineId).NotEmpty().WithMessage("VaccineIdRequired");
            RuleFor(x => x.Title).NotEmpty().WithMessage("TitleRequired");
            RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).WithMessage("EndDateBeforeStartDate");
        }
    }
}
