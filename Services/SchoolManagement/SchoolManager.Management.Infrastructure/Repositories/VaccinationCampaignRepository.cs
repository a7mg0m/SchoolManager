using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class VaccinationCampaignRepository : BaseRepository<VaccinationCampaign>, IVaccinationCampaignRepository
	{
		public VaccinationCampaignRepository(ManagementDbContext context) : base(context)
		{
		}
	}
}
