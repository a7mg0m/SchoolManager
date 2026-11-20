using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record GetStudentFeeByIdQuery(Guid Id) : IQuery<StudentFeeDto?>;

    /// <param name="AcademicYearId">Only that year's fees (and those assigned without a year); null = all</param>
    public record GetStudentFeesByStudentQuery(Guid StudentId, Guid? AcademicYearId = null) : IQuery<List<StudentFeeDto>>;

    /// <param name="AcademicYearId">Only that year's fees (and those assigned without a year); null = all</param>
    public record GetAllStudentFeesQuery(Guid? AcademicYearId = null) : IQuery<List<StudentFeeDto>>;
}
