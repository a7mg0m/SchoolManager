using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateStudentCommand(
        string FullNameAr,
        string FullNameEn,
        DateOnly DateOfBirth,
        string NationalId,
        string Gender,
        string PhoneNumber,
        string? Email,
        string Address,
        Guid GradeId,
        Guid SectionId,
        Guid AcademicYearId,
        Guid ParentId,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
