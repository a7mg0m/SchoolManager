using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class GetMyParentsQueryHandler(
        ISubjectRepository subjects,
        ISectionRepository sections,
        IStudentRepository students,
        IParentRepository parents) : IQueryHandler<GetMyParentsQuery, List<TeacherParentDto>>
    {
        public async Task<List<TeacherParentDto>> Handle(GetMyParentsQuery query, CancellationToken cancellationToken)
        {
            // The teacher's classes: the active classes of the grades they teach (as in my-classes)
            var taught = (await subjects.GetForTeacherAsync(query.TeacherId, cancellationToken)).Where(s => s.IsActive).ToList();
            var gradeNames = taught.Where(s => s.Grade is not null)
                .GroupBy(s => s.GradeId)
                .ToDictionary(g => g.Key, g => g.First().Grade!.Name);
            var gradeIds = gradeNames.Keys.ToList();
            if (gradeIds.Count == 0)
                return new List<TeacherParentDto>();

            var classes = (await sections.SearchAsync(s => gradeIds.Contains(s.GradeId) && s.IsActive)).ToDictionary(s => s.Id);
            var sectionIds = classes.Keys.ToList();
            var classStudents = await students.SearchAsync(s => sectionIds.Contains(s.SectionId) && s.IsActive);
            var parentIds = classStudents.Select(s => s.ParentId).Distinct().ToList();
            if (parentIds.Count == 0)
                return new List<TeacherParentDto>();

            var found = await parents.SearchAsync(p => parentIds.Contains(p.Id));
            return found
                .Select(p => new TeacherParentDto
                {
                    Id = p.Id,
                    NameAr = p.FullName.Arabic,
                    NameEn = p.FullName.English,
                    PhoneNumber = p.PhoneNumber,
                    Email = p.Email,
                    Children = classStudents
                        .Where(s => s.ParentId == p.Id)
                        .OrderBy(s => s.FullName.Arabic)
                        .Select(s =>
                        {
                            var section = classes[s.SectionId];
                            var grade = gradeNames.GetValueOrDefault(section.GradeId);
                            return new TeacherParentChildDto
                            {
                                StudentId = s.Id,
                                NameAr = s.FullName.Arabic,
                                NameEn = s.FullName.English,
                                SectionId = s.SectionId,
                                SectionNameAr = section.Name.Arabic,
                                SectionNameEn = section.Name.English,
                                GradeNameAr = grade?.Arabic ?? string.Empty,
                                GradeNameEn = grade?.English ?? string.Empty
                            };
                        })
                        .ToList()
                })
                .OrderBy(p => p.NameAr)
                .ToList();
        }
    }
}
