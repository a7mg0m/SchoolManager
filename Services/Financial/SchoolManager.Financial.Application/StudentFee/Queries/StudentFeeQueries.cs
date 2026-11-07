using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record GetStudentFeeByIdQuery(Guid Id) : IQuery<StudentFeeDto?>;

    public record GetStudentFeesByStudentQuery(Guid StudentId) : IQuery<List<StudentFeeDto>>;

    public record GetAllStudentFeesQuery() : IQuery<List<StudentFeeDto>>;
}
