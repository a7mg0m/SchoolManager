using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetStudentMedicalProfileByStudentIdQuery(Guid StudentId) : ICommand<StudentMedicalProfileDto?>;
}
