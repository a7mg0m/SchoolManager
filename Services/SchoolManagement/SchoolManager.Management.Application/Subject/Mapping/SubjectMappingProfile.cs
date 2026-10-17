using AutoMapper;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class SubjectMappingProfile : Profile
    {
        public SubjectMappingProfile()
        {
            CreateMap<CreateSubjectDto, CreateSubjectCommand>()
                .ConstructUsing(src => new CreateSubjectCommand(
                    src.NameAr,
                    src.NameEn,
                    src.GradeId,
                    src.TeacherIds,
                    Guid.NewGuid()));

            CreateMap<UpdateSubjectDto, UpdateSubjectCommand>()
                .ConstructUsing(src => new UpdateSubjectCommand(
                    src.Id,
                    src.NameAr,
                    src.NameEn,
                    src.GradeId,
                    src.TeacherIds,
                    Guid.NewGuid()));

            CreateMap<Subject, SubjectDto>()
                .ForMember(d => d.NameAr, o => o.MapFrom(s => s.Name.Arabic))
                .ForMember(d => d.NameEn, o => o.MapFrom(s => s.Name.English))
                .ForMember(d => d.TeacherIds, o => o.MapFrom(s => s.SubjectTeachers.Select(t => t.TeacherId)));
        }
    }
}
