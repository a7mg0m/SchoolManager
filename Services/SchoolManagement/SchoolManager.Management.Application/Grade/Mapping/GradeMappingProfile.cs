using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class GradeMappingProfile : Profile
	{
		public GradeMappingProfile()
		{
			CreateMap<CreateGradeDto, CreateGradeCommand>()
				.ConstructUsing(src => new CreateGradeCommand(
					src.NameAr,
                    src.NameEn,
                    src.StageId,
                    Guid.NewGuid()));

            CreateMap<UpdateGradeDto, UpdateGradeCommand>()
                .ConstructUsing(src => new UpdateGradeCommand(
                    src.Id,
                    src.NameAr,
                    src.NameEn,
                    src.StageId,
                    Guid.NewGuid()));

			CreateMap<Grade, GradeDto>()
                .ForMember(d => d.NameAr, o => o.MapFrom(s => s.Name.Arabic))
                .ForMember(d => d.NameEn, o => o.MapFrom(s => s.Name.English));
		}
	}
}
