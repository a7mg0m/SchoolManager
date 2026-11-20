using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class StudentVaccinationMappingProfile : Profile
	{
		public StudentVaccinationMappingProfile()
		{
			CreateMap<CreateStudentVaccinationDto, CreateStudentVaccinationCommand>()
				.ConstructUsing(src => new CreateStudentVaccinationCommand(src.StudentId, src.VaccineId, src.DoseNumber, src.Status, src.VaccinationDate, src.Place, src.CampaignId, src.GivenByEmployeeId, src.Notes));

			CreateMap<UpdateStudentVaccinationDto, UpdateStudentVaccinationCommand>()
				.ConstructUsing(src => new UpdateStudentVaccinationCommand(src.Id, src.VaccineId, src.DoseNumber, src.Status, src.VaccinationDate, src.Place, src.CampaignId, src.GivenByEmployeeId, src.Notes));

			CreateMap<StudentVaccination, StudentVaccinationDto>()
				.ForMember(d => d.VaccineNameAr, o => o.MapFrom(s => s.Vaccine != null ? s.Vaccine.Name.Arabic : string.Empty))
				.ForMember(d => d.VaccineNameEn, o => o.MapFrom(s => s.Vaccine != null ? s.Vaccine.Name.English : string.Empty));
		}
	}
}
