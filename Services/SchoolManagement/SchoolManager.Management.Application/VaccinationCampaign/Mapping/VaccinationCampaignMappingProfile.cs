using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class VaccinationCampaignMappingProfile : Profile
	{
		public VaccinationCampaignMappingProfile()
		{
			CreateMap<CreateVaccinationCampaignDto, CreateVaccinationCampaignCommand>()
				.ConstructUsing(src => new CreateVaccinationCampaignCommand(src.VaccineId, src.Title, src.StartDate, src.EndDate));

			CreateMap<UpdateVaccinationCampaignDto, UpdateVaccinationCampaignCommand>()
				.ConstructUsing(src => new UpdateVaccinationCampaignCommand(src.Id, src.VaccineId, src.Title, src.StartDate, src.EndDate));

			CreateMap<VaccinationCampaign, VaccinationCampaignDto>();
		}
	}
}
