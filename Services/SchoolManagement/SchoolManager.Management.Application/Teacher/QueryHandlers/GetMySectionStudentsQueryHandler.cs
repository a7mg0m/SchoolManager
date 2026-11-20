using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class GetMySectionStudentsQueryHandler(
        ISubjectRepository subjects,
        ISectionRepository sections,
        IStudentRepository students) : IQueryHandler<GetMySectionStudentsQuery, List<TeacherStudentDto>?>
    {
        public async Task<List<TeacherStudentDto>?> Handle(GetMySectionStudentsQuery query, CancellationToken cancellationToken)
        {
            var section = (await sections.SearchAsync(s => s.Id == query.SectionId)).FirstOrDefault();
            if (section is null)
                return null;

            // Only classes of a grade the teacher teaches
            var teacherSubjects = await subjects.GetForTeacherAsync(query.TeacherId, cancellationToken);
            if (!teacherSubjects.Any(s => s.IsActive && s.GradeId == section.GradeId))
                return null;

            return (await students.SearchAsync(s => s.SectionId == query.SectionId && s.IsActive))
                .OrderBy(s => s.FullName.Arabic)
                .Select(s => new TeacherStudentDto
                {
                    Id = s.Id,
                    NameAr = s.FullName.Arabic,
                    NameEn = s.FullName.English,
                    Gender = s.Gender,
                    DateOfBirth = s.DateOfBirth
                })
                .ToList();
        }
    }
}
