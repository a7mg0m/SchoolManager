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
        double? Latitude = null,
        double? Longitude = null
    ) : ICommand<bool>;
}
