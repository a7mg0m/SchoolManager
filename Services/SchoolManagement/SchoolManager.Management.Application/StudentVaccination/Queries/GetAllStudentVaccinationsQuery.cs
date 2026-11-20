using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetAllStudentVaccinationsQuery() : ICommand<List<StudentVaccinationDto>>;
}
