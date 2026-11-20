using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetStudentVaccinationsByStudentIdQuery(Guid StudentId) : ICommand<List<StudentVaccinationDto>>;
}
