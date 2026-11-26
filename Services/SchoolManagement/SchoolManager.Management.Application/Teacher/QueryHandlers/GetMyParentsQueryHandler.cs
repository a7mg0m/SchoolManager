using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class GetMyParentsQueryHandler(
        ISubjectRepository subjects,
        ISectionRepository sections,
        IStudentRepository students,
        IParentRepository parents,
        ITimetableRepository timetables) : IQueryHandler<GetMyParentsQuery, List<TeacherParentDto>>
    {
        public async Task<List<TeacherParentDto>> Handle(GetMyParentsQuery query, CancellationToken cancellationToken)
        {
            Dictionary<Guid, Section> classes;
            Dictionary<Guid, NameLocalized> gradeNames;

            if (query.FromTimetable && query.AcademicYearId is Guid yearId && yearId != Guid.Empty)
            {
                // Chat: only the classes the teacher teaches in the chosen year / semester's timetable
                var lessons = (await timetables.GetTeacherSlotsAsync(query.TeacherId, yearId, query.SemesterId, cancellationToken))
                    .Where(s => s.Section is not null && s.Section.IsActive)
                    .ToList();
                classes = lessons.GroupBy(s => s.SectionId).ToDictionary(g => g.Key, g => g.First().Section!);
                gradeNames = lessons.Where(s => s.Grade is not null)
                    .GroupBy(s => s.GradeId)
                    .ToDictionary(g => g.Key, g => g.First().Grade!.Name);
            }
            else
            {
                // The teacher's classes: the active classes of the grades they teach (as in my-classes)
                var taught = (await subjects.GetForTeacherAsync(query.TeacherId, cancellationToken)).Where(s => s.IsActive).ToList();
                gradeNames = taught.Where(s => s.Grade is not null)
                    .GroupBy(s => s.GradeId)
                    .ToDictionary(g => g.Key, g => g.First().Grade!.Name);
                var gradeIds = gradeNames.Keys.ToList();
                if (gradeIds.Count == 0)
                    return new List<TeacherParentDto>();
                classes = (await sections.SearchAsync(s => gradeIds.Contains(s.GradeId) && s.IsActive)).ToDictionary(s => s.Id);
            }

            var sectionIds = classes.Keys.ToList();
            if (sectionIds.Count == 0)
                return new List<TeacherParentDto>();
            var classStudents = await students.SearchAsync(s => s.SectionId != null && sectionIds.Contains(s.SectionId.Value) && s.IsActive);
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
                            var section = classes[s.SectionId!.Value];
                            var grade = gradeNames.GetValueOrDefault(section.GradeId);
                            return new TeacherParentChildDto
                            {
                                StudentId = s.Id,
                                NameAr = s.FullName.Arabic,
                                NameEn = s.FullName.English,
                                SectionId = s.SectionId!.Value,
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
