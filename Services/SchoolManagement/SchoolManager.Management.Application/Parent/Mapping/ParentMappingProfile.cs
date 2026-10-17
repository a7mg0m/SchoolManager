using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
	public class ParentMappingProfile : Profile
	{
		public ParentMappingProfile()
		{
			CreateMap<CreateParentDto, CreateParentCommand>()
				.ConstructUsing(src => new CreateParentCommand(
					src.FullNameAr,
                    src.FullNameEn,
                    src.PhoneNumber,
                    src.Email,
                    src.Address,
                    Guid.NewGuid()));

            CreateMap<UpdateParentDto, UpdateParentCommand>()
                .ConstructUsing(src => new UpdateParentCommand(
                    src.Id,
                    src.FullNameAr,
                    src.FullNameEn,
                    src.PhoneNumber,
                    src.Email,
                    src.Address,
                    Guid.NewGuid()));

			CreateMap<Parent, ParentDto>()
                .ForMember(d => d.FullNameAr, o => o.MapFrom(s => s.FullName.Arabic))
                .ForMember(d => d.FullNameEn, o => o.MapFrom(s => s.FullName.English));
		}
	}
}
