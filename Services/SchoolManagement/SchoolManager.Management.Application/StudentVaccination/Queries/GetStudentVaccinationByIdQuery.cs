using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record GetStudentVaccinationByIdQuery(Guid Id) : ICommand<StudentVaccinationDto?>;
}
