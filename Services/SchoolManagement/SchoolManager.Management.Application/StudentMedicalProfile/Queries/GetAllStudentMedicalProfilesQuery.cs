using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAllStudentMedicalProfilesQuery() : ICommand<List<StudentMedicalProfileDto>>;
}
