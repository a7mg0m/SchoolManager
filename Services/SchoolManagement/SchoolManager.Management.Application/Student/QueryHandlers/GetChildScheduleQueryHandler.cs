using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class GetChildScheduleQueryHandler : ICommandHandler<GetChildScheduleQuery, ChildScheduleDto?>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ITimetableRepository _timetableRepository;

        public GetChildScheduleQueryHandler(IStudentRepository studentRepository, ITimetableRepository timetableRepository)
        {
            _studentRepository = studentRepository;
            _timetableRepository = timetableRepository;
        }

        public async Task<ChildScheduleDto?> Handle(GetChildScheduleQuery request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId);
            if (student is null || student.ParentId != request.ParentId)
                return null;

            var slots = await _timetableRepository.GetSectionSlotsAsync(
                student.AcademicYearId, student.GradeId, student.SectionId, cancellationToken);

            var today = DateOnly.FromDateTime(request.Now);
            var now = TimeOnly.FromDateTime(request.Now);

            // Today while it still has a lesson that has not ended; otherwise the next day with lessons
            var todaySlots = SlotsOf(slots, today.DayOfWeek);
            if (todaySlots.Any(s => s.EndTime > now))
                return Build(today, now, isToday: true, todaySlots);

            for (var offset = 1; offset <= 7; offset++)
            {
                var day = today.AddDays(offset);
                var daySlots = SlotsOf(slots, day.DayOfWeek);
                if (daySlots.Count > 0)
                    return Build(day, now, isToday: false, daySlots);
            }

            // No timetable for the section (or no lessons on any day)
            return new ChildScheduleDto { Date = today, DayOfWeek = today.DayOfWeek, IsToday = true, Now = now };
        }

        private static List<TimetableSlot> SlotsOf(List<TimetableSlot> slots, DayOfWeek day)
            => slots.Where(s => s.Day == day).OrderBy(s => s.StartTime).ToList();

        private static ChildScheduleDto Build(DateOnly date, TimeOnly now, bool isToday, List<TimetableSlot> slots)
        {
            var result = new ChildScheduleDto
            {
                Date = date,
                DayOfWeek = date.DayOfWeek,
                IsToday = isToday,
                Now = now
            };

            foreach (var slot in slots)
            {
                var status = !isToday || slot.StartTime > now ? SlotStatus.Upcoming
                    : slot.EndTime <= now ? SlotStatus.Finished
                    : SlotStatus.Current;

                if (status == SlotStatus.Current)
                    result.CurrentSlotId ??= slot.Id;
                if (status == SlotStatus.Upcoming)
                    result.NextSlotId ??= slot.Id;

                result.Slots.Add(new ChildScheduleSlotDto
                {
                    Id = slot.Id,
                    NameAr = slot.Name.Arabic,
                    NameEn = slot.Name.English,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    SubjectNameAr = slot.Subject?.Name.Arabic ?? string.Empty,
                    SubjectNameEn = slot.Subject?.Name.English ?? string.Empty,
                    TeacherNameAr = slot.Teacher?.FullName.Arabic ?? string.Empty,
                    TeacherNameEn = slot.Teacher?.FullName.English ?? string.Empty,
                    Status = status
                });
            }

            return result;
        }
    }
}
