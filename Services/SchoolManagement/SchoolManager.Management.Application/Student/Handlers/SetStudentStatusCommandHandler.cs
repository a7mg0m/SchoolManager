using BuildingBlocks;
using BuildingBlocks.CQRS;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Application
{
    public class SetStudentStatusCommandHandler(ICurrentUserService currentUser, IStudentRepository students, IUnitOfWork unitOfWork)
        : ICommandHandler<SetStudentStatusCommand, bool>
    {
        public async Task<bool> Handle(SetStudentStatusCommand command, CancellationToken cancellationToken)
        {
            var student = await students.GetByIdAsync(command.Id);
            if (student is null) return false;

            student.ChangeStatus(command.Status, command.Date, command.Note, new AuditInfo(DateTime.Now, currentUser.UserId));
            await unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
