using BuildingBlocks.CQRS;

namespace SchoolManager.Management.Application
{
    public record DeleteStudentMedicalProfileCommand(Guid Id) : ICommand<bool>;
}
