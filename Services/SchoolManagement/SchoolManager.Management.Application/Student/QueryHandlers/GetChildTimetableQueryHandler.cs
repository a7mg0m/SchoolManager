using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class GetChildTimetableQueryHandler : ICommandHandler<GetChildTimetableQuery, ChildTimetableDto?>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ITimetableRepository _timetableRepository;

        public GetChildTimetableQueryHandler(IStudentRepository studentRepository, ITimetableRepository timetableRepository)
        {
            _studentRepository = studentRepository;
            _timetableRepository = timetableRepository;
        }

        public async Task<ChildTimetableDto?> Handle(GetChildTimetableQuery request, CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId);
            if (student is null || student.ParentId != request.ParentId)
                return null;

            var slots = await _timetableRepository.GetSectionSlotsAsync(
                student.AcademicYearId, student.GradeId, student.SectionId, cancellationToken);

            return new ChildTimetableDto
            {
                StudentId = student.Id,
                Today = request.Now.DayOfWeek,
                Now = TimeOnly.FromDateTime(request.Now),
                Days = slots
                    .GroupBy(s => s.Day)
                    // Saturday first: fits both Saturday-Thursday and Sunday-Thursday school weeks
                    .OrderBy(g => ((int)g.Key + 1) % 7)
                    .Select(g => new ChildTimetableDayDto
                    {
                        DayOfWeek = g.Key,
                        Slots = g.OrderBy(s => s.StartTime).Select(ToDto).ToList()
                    })
                    .ToList()
            };
        }

        private static ChildScheduleSlotDto ToDto(TimetableSlot slot) => new()
        {
            Id = slot.Id,
            NameAr = slot.Name.Arabic,
            NameEn = slot.Name.English,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            SubjectNameAr = slot.Subject?.Name.Arabic ?? string.Empty,
            SubjectNameEn = slot.Subject?.Name.English ?? string.Empty,
            TeacherNameAr = slot.Teacher?.FullName.Arabic ?? string.Empty,
            TeacherNameEn = slot.Teacher?.FullName.English ?? string.Empty
            // Status is computed by the app from Today / Now
        };
    }
}
