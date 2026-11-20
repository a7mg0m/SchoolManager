using BuildingBlocks.CQRS;

namespace SchoolManager.ExamsHomework.Application
{
    /// <summary>The logged-in teacher's homework (names of subjects, grades and classes come from Classes)</summary>
    public record GetTeacherHomeworksQuery(Guid TeacherId, TeacherClasses Classes, Guid? AcademicYearId = null, Guid? SemesterId = null) : IQuery<List<TeacherHomeworkDto>>;
}
