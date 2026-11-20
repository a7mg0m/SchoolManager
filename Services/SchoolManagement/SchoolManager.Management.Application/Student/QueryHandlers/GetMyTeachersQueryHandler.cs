using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class GetMyTeachersQueryHandler(
        IStudentRepository students,
        ISubjectRepository subjects,
        ITimetableSlotRepository slots,
        ITeacherRepository teachers) : IQueryHandler<GetMyTeachersQuery, List<ParentTeacherDto>>
    {
        public async Task<List<ParentTeacherDto>> Handle(GetMyTeachersQuery query, CancellationToken cancellationToken)
        {
            var children = (await students.GetByParentWithAcademicInfoAsync(query.ParentId, cancellationToken))
                .Where(s => s.IsActive)
                .ToList();
            if (children.Count == 0)
                return new List<ParentTeacherDto>();

            var gradeIds = children.Select(c => c.GradeId).Distinct().ToList();
            // Subjects of the children's grades, with their linked teachers
            var gradeSubjects = (await subjects.GetAllAsync())
                .Where(s => s.IsActive && gradeIds.Contains(s.GradeId))
                .ToList();
            var subjectIds = gradeSubjects.Select(s => s.Id).ToList();
            var taughtInTimetable = subjectIds.Count == 0
                ? new List<TimetableSlot>()
                : await slots.SearchAsync(slot => subjectIds.Contains(slot.SubjectId));

            // (teacher, subject) pairs, as in the teacher's "my classes"
            var pairs = gradeSubjects
                .SelectMany(s => s.SubjectTeachers.Select(t => (TeacherId: t.TeacherId, Subject: s)))
                .Concat(taughtInTimetable.Select(slot => (slot.TeacherId, Subject: gradeSubjects.First(s => s.Id == slot.SubjectId))))
                .DistinctBy(p => (p.TeacherId, p.Subject.Id))
                .ToList();
            var teacherIds = pairs.Select(p => p.TeacherId).Distinct().ToList();
            if (teacherIds.Count == 0)
                return new List<ParentTeacherDto>();

            var found = await teachers.SearchAsync(t => teacherIds.Contains(t.Id) && t.IsActive);
            return found
                .Select(t =>
                {
                    var taught = pairs.Where(p => p.TeacherId == t.Id).Select(p => p.Subject).ToList();
                    var taughtGrades = taught.Select(s => s.GradeId).ToHashSet();
                    return new ParentTeacherDto
                    {
                        TeacherId = t.Id,
                        NameAr = t.FullName.Arabic,
                        NameEn = t.FullName.English,
                        Subjects = taught
                            .OrderBy(s => s.Name.Arabic)
                            .Select(s => new ParentTeacherSubjectDto { SubjectId = s.Id, NameAr = s.Name.Arabic, NameEn = s.Name.English })
                            .ToList(),
                        Children = children
                            .Where(c => taughtGrades.Contains(c.GradeId))
                            .Select(c => new ParentTeacherChildDto { StudentId = c.Id, NameAr = c.FullName.Arabic, NameEn = c.FullName.English })
                            .ToList()
                    };
                })
                .OrderBy(t => t.NameAr)
                .ToList();
        }
    }
}
