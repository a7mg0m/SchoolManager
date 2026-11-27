using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    /// <summary>When and where a lesson of a timetable being saved is, and who teaches it</summary>
    public record TimetableSlotTime(DayOfWeek Day, TimeOnly Start, TimeOnly End, Guid TeacherId, Guid SectionId);

    /// <summary>
    /// A teacher booked twice at the same time: the lesson being saved (Day / times / SectionId) and the other one
    /// (in this timetable, or in another grade's timetable of the same year and semester).
    /// </summary>
    public record TeacherClashDto(
        Guid TeacherId,
        string TeacherNameAr,
        string TeacherNameEn,
        DayOfWeek Day,
        TimeOnly StartTime,
        TimeOnly EndTime,
        string SectionNameAr,
        string SectionNameEn,
        string OtherGradeNameAr,
        string OtherGradeNameEn,
        string OtherSectionNameAr,
        string OtherSectionNameEn,
        TimeOnly OtherStartTime,
        TimeOnly OtherEndTime);

    /// <summary>A timetable that books a teacher in two lessons at once (the API answers 400 "TeacherClash")</summary>
    public class TeacherClashException(List<TeacherClashDto> clashes) : Exception("TeacherClash")
    {
        public List<TeacherClashDto> Clashes { get; } = clashes;
    }

    /// <summary>A teacher cannot give two lessons at the same time (in two sections, or two grades)</summary>
    public static class TeacherClashes
    {
        private const int MaxReported = 20;

        public static async Task EnsureNoneAsync(
            ITimetableRepository timetables,
            ITeacherRepository teachers,
            ISectionRepository sections,
            IGradeRepository grades,
            Guid academicYearId,
            Guid? semesterId,
            Guid gradeId,
            Guid? selfTimetableId,
            IReadOnlyList<TimetableSlotTime> slots,
            CancellationToken cancellationToken)
        {
            var mine = slots.Where(s => s.TeacherId != Guid.Empty).ToList();
            if (mine.Count == 0) return;

            // (lesson being saved, the other lesson: same teacher, same day, overlapping)
            var found = new List<(TimetableSlotTime Slot, Guid OtherGradeId, Guid OtherSectionId, TimeOnly OtherStart, TimeOnly OtherEnd)>();

            for (var i = 0; i < mine.Count; i++)
                for (var j = i + 1; j < mine.Count; j++)
                    if (Clash(mine[i], mine[j].TeacherId, mine[j].Day, mine[j].Start, mine[j].End))
                        found.Add((mine[i], gradeId, mine[j].SectionId, mine[j].Start, mine[j].End));

            var teacherIds = mine.Select(s => s.TeacherId).Distinct().ToList();
            var others = await timetables.GetTeachersOtherSlotsAsync(teacherIds, academicYearId, semesterId, selfTimetableId, cancellationToken);
            foreach (var slot in mine)
                foreach (var other in others)
                    if (Clash(slot, other.TeacherId, other.Day, other.StartTime, other.EndTime))
                        found.Add((slot, other.GradeId, other.SectionId, other.StartTime, other.EndTime));

            if (found.Count == 0) return;
            found = found.Take(MaxReported).ToList();

            // Names for the message
            var tIds = found.Select(f => f.Slot.TeacherId).Distinct().ToList();
            var teacherNames = (await teachers.SearchAsync(t => tIds.Contains(t.Id))).ToDictionary(t => t.Id, t => t.FullName);
            var sIds = found.SelectMany(f => new[] { f.Slot.SectionId, f.OtherSectionId }).Distinct().ToList();
            var sectionNames = (await sections.SearchAsync(s => sIds.Contains(s.Id))).ToDictionary(s => s.Id, s => s.Name);
            var gIds = found.Select(f => f.OtherGradeId).Distinct().ToList();
            var gradeNames = (await grades.SearchAsync(g => gIds.Contains(g.Id))).ToDictionary(g => g.Id, g => g.Name);

            throw new TeacherClashException(found.Select(f =>
            {
                var teacher = teacherNames.GetValueOrDefault(f.Slot.TeacherId);
                var section = sectionNames.GetValueOrDefault(f.Slot.SectionId);
                var otherGrade = gradeNames.GetValueOrDefault(f.OtherGradeId);
                var otherSection = sectionNames.GetValueOrDefault(f.OtherSectionId);
                return new TeacherClashDto(
                    f.Slot.TeacherId, teacher?.Arabic ?? "", teacher?.English ?? "",
                    f.Slot.Day, f.Slot.Start, f.Slot.End,
                    section?.Arabic ?? "", section?.English ?? "",
                    otherGrade?.Arabic ?? "", otherGrade?.English ?? "",
                    otherSection?.Arabic ?? "", otherSection?.English ?? "",
                    f.OtherStart, f.OtherEnd);
            }).ToList());
        }

        private static bool Clash(TimetableSlotTime a, Guid teacherId, DayOfWeek day, TimeOnly start, TimeOnly end) =>
            a.TeacherId == teacherId && a.Day == day && a.Start < end && start < a.End;
    }
}
