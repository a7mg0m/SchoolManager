using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class StudentMedicalProfileMappingProfile : Profile
	{
		public StudentMedicalProfileMappingProfile()
		{
			CreateMap<SaveStudentMedicalProfileDto, SaveStudentMedicalProfileCommand>()
				.ConstructUsing(src => new SaveStudentMedicalProfileCommand(
					src.StudentId, src.BloodType, src.Allergies, src.ChronicDiseases, src.RegularMedications, src.EmergencyPhone, src.Notes));

			CreateMap<StudentMedicalProfile, StudentMedicalProfileDto>();
		}
	}
}
