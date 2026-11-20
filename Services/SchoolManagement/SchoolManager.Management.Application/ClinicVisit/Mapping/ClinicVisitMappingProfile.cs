using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class ClinicVisitMappingProfile : Profile
	{
		public ClinicVisitMappingProfile()
		{
			CreateMap<CreateClinicVisitDto, CreateClinicVisitCommand>()
				.ConstructUsing(src => new CreateClinicVisitCommand(src.StudentId, src.DoctorName, src.VisitDate, src.Complaint, src.Temperature, src.Weight, src.Height, src.Diagnosis, src.Treatment, src.Outcome, src.ParentNotified, src.Notes, src.Medications ?? new List<VisitMedicationDto>()));

			CreateMap<UpdateClinicVisitDto, UpdateClinicVisitCommand>()
				.ConstructUsing(src => new UpdateClinicVisitCommand(src.Id, src.DoctorName, src.VisitDate, src.Complaint, src.Temperature, src.Weight, src.Height, src.Diagnosis, src.Treatment, src.Outcome, src.ParentNotified, src.Notes, src.Medications ?? new List<VisitMedicationDto>()));

			CreateMap<VisitMedication, VisitMedicationDto>();
			CreateMap<ClinicVisit, ClinicVisitDto>();
		}
	}
}
