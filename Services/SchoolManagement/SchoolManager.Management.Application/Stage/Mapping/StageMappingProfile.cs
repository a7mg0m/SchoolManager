using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class StageMappingProfile : Profile
	{
		public StageMappingProfile()
		{
			CreateMap<CreateStageDto, CreateStageCommand>()
				.ConstructUsing(src => new CreateStageCommand(
					src.NameAr,
                    src.NameEn,
                    Guid.NewGuid()));

            CreateMap<UpdateStageDto, UpdateStageCommand>()
                .ConstructUsing(src => new UpdateStageCommand(
                    src.Id,
                    src.NameAr,
                    src.NameEn,
                    Guid.NewGuid()));

			CreateMap<Stage, StageDto>()
                .ForMember(d => d.NameAr, o => o.MapFrom(s => s.Name.Arabic))
                .ForMember(d => d.NameEn, o => o.MapFrom(s => s.Name.English));
		}
	}
}
