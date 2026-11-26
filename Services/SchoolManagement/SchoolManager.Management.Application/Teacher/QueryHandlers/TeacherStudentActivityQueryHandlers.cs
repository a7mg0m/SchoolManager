using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>Does the teacher teach this student? Shared by the two queries below.</summary>
    internal static class TaughtStudents
    {
        /// <summary>The student (with stage / grade / section) when the teacher teaches their class; else null</summary>
        public static async Task<Student?> FindAsync(
            IStudentRepository students, ISubjectRepository subjects, ITimetableSlotRepository slots,
            Guid teacherId, Guid studentId, CancellationToken cancellationToken)
        {
            var student = await students.GetByIdWithAcademicInfoAsync(studentId, cancellationToken);
            if (student is null || !student.IsActive) return null;

            // A subject of the student's grade (as in my-classes / my-students)…
            var teacherSubjects = await subjects.GetForTeacherAsync(teacherId, cancellationToken);
            if (teacherSubjects.Any(s => s.IsActive && s.GradeId == student.GradeId)) return student;

            // …or lessons in the student's class timetable
            var lessons = await slots.SearchAsync(s => s.TeacherId == teacherId && s.SectionId == student.SectionId);
            return lessons.Count > 0 ? student : null;
        }

        public static TaughtStudentDto ToDto(Student s) => new()
        {
            Id = s.Id,
            NameAr = s.FullName.Arabic,
            NameEn = s.FullName.English,
            Gender = s.Gender,
            DateOfBirth = s.DateOfBirth,
            StageId = s.StageId,
            GradeId = s.GradeId,
            SectionId = s.SectionId,
            AcademicYearId = s.AcademicYearId,
            StageNameAr = s.Stage?.Name.Arabic ?? string.Empty,
            StageNameEn = s.Stage?.Name.English ?? string.Empty,
            GradeNameAr = s.Grade?.Name.Arabic ?? string.Empty,
            GradeNameEn = s.Grade?.Name.English ?? string.Empty,
            SectionNameAr = s.Section?.Name.Arabic ?? string.Empty,
            SectionNameEn = s.Section?.Name.English ?? string.Empty
        };
    }

    public class GetTaughtStudentQueryHandler(
        IStudentRepository students,
        ISubjectRepository subjects,
        ITimetableSlotRepository slots) : IQueryHandler<GetTaughtStudentQuery, TaughtStudentDto?>
    {
        public async Task<TaughtStudentDto?> Handle(GetTaughtStudentQuery query, CancellationToken cancellationToken)
        {
            var student = await TaughtStudents.FindAsync(students, subjects, slots, query.TeacherId, query.StudentId, cancellationToken);
            return student is null ? null : TaughtStudents.ToDto(student);
        }
    }

    public class GetTeacherStudentActivityQueryHandler(
        IStudentRepository students,
        ISubjectRepository subjects,
        ITimetableSlotRepository slots,
        ITimetableRepository timetables,
        ITeacherRepository teachers) : IQueryHandler<GetTeacherStudentActivityQuery, TeacherStudentActivityDto?>
    {
        public async Task<TeacherStudentActivityDto?> Handle(GetTeacherStudentActivityQuery query, CancellationToken cancellationToken)
        {
            var student = await TaughtStudents.FindAsync(students, subjects, slots, query.TeacherId, query.StudentId, cancellationToken);
            if (student is null) return null;

            // The class timetable of the year / semester chosen in the app (the student's year when none)
            var yearId = query.AcademicYearId is Guid y && y != Guid.Empty ? y : student.AcademicYearId;
            var classSlots = await timetables.GetSectionSlotsAsync(yearId, query.SemesterId, student.GradeId, student.SectionId ?? Guid.Empty, cancellationToken);
            var now = DateTime.Now;
            var timetable = new ChildTimetableDto
            {
                StudentId = student.Id,
                Today = now.DayOfWeek,
                Now = TimeOnly.FromDateTime(now),
                Days = classSlots
                    .GroupBy(s => s.Day)
                    .OrderBy(g => ((int)g.Key + 1) % 7) // Saturday first
                    .Select(g => new ChildTimetableDayDto
                    {
                        DayOfWeek = g.Key,
                        Slots = g.OrderBy(s => s.StartTime).Select(s => new ChildScheduleSlotDto
                        {
                            Id = s.Id,
                            NameAr = s.Name.Arabic,
                            NameEn = s.Name.English,
                            StartTime = s.StartTime,
                            EndTime = s.EndTime,
                            SubjectNameAr = s.Subject?.Name.Arabic ?? string.Empty,
                            SubjectNameEn = s.Subject?.Name.English ?? string.Empty,
                            TeacherNameAr = s.Teacher?.FullName.Arabic ?? string.Empty,
                            TeacherNameEn = s.Teacher?.FullName.English ?? string.Empty
                        }).ToList()
                    })
                    .ToList()
            };

            // Teachers of the grade's subjects (linked, or teaching it in the class timetable): names for the reviews
            var gradeSubjects = (await subjects.GetAllAsync()).Where(s => s.IsActive && s.GradeId == student.GradeId).ToList();
            var pairs = gradeSubjects
                .SelectMany(s => s.SubjectTeachers.Select(t => (TeacherId: t.TeacherId, Subject: s)))
                .Concat(classSlots.Where(s => s.Subject is not null).Select(s => (s.TeacherId, Subject: s.Subject!)))
                .DistinctBy(p => (p.TeacherId, p.Subject.Id))
                .ToList();
            var teacherIds = pairs.Select(p => p.TeacherId).Distinct().ToList();
            var found = teacherIds.Count == 0 ? new List<Teacher>() : await teachers.SearchAsync(t => teacherIds.Contains(t.Id));

            return new TeacherStudentActivityDto
            {
                Student = TaughtStudents.ToDto(student),
                Timetable = timetable,
                Teachers = found
                    .Select(t => new ParentTeacherDto
                    {
                        TeacherId = t.Id,
                        NameAr = t.FullName.Arabic,
                        NameEn = t.FullName.English,
                        Subjects = pairs.Where(p => p.TeacherId == t.Id)
                            .Select(p => new ParentTeacherSubjectDto { SubjectId = p.Subject.Id, NameAr = p.Subject.Name.Arabic, NameEn = p.Subject.Name.English })
                            .ToList()
                    })
                    .OrderBy(t => t.NameAr)
                    .ToList()
            };
        }
    }
}
