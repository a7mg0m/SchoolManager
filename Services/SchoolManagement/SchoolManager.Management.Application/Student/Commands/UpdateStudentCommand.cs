using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record UpdateStudentCommand(
        Guid Id,
        string FullNameAr,
        string FullNameEn,
        string PhoneNumber,
        string? Email,
        string Address,
        Guid GradeId,
        Guid SectionId,
        Guid AcademicYearId,
        Guid UpdatedBy
    ) : ICommand<bool>;
}
