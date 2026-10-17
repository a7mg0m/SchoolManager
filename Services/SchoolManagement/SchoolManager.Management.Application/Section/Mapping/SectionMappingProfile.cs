using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class SectionMappingProfile : Profile
	{
		public SectionMappingProfile()
		{
			CreateMap<CreateSectionDto, CreateSectionCommand>()
				.ConstructUsing(src => new CreateSectionCommand(
					src.NameAr,
                    src.NameEn,
                    src.GradeId,
                    Guid.NewGuid()));

            CreateMap<UpdateSectionDto, UpdateSectionCommand>()
                .ConstructUsing(src => new UpdateSectionCommand(
                    src.Id,
                    src.NameAr,
                    src.NameEn,
                    src.GradeId,
                    Guid.NewGuid()));

			CreateMap<Section, SectionDto>()
                .ForMember(d => d.NameAr, o => o.MapFrom(s => s.Name.Arabic))
                .ForMember(d => d.NameEn, o => o.MapFrom(s => s.Name.English));
		}
	}
}
