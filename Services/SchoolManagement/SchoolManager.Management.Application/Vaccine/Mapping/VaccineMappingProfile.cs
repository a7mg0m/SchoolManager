using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class VaccineMappingProfile : Profile
	{
		public VaccineMappingProfile()
		{
			CreateMap<CreateVaccineDto, CreateVaccineCommand>()
				.ConstructUsing(src => new CreateVaccineCommand(src.NameAr, src.NameEn, src.TotalDoses, src.IsMandatory));

			CreateMap<UpdateVaccineDto, UpdateVaccineCommand>()
				.ConstructUsing(src => new UpdateVaccineCommand(src.Id, src.NameAr, src.NameEn, src.TotalDoses, src.IsMandatory));

			CreateMap<Vaccine, VaccineDto>()
				.ForMember(d => d.NameAr, o => o.MapFrom(s => s.Name.Arabic))
				.ForMember(d => d.NameEn, o => o.MapFrom(s => s.Name.English));
		}
	}
}
