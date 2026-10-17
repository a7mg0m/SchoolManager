using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record CreateTeacherCommand(
        string FullNameAr,
        string FullNameEn,
        DateOnly DateOfBirth,
        string NationalId,
        string Gender,
        string PhoneNumber,
        string? Email,
        string Address,
        Guid CreatedBy
    ) : ICommand<Guid>;
}
