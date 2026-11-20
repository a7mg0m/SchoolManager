using BuildingBlocks;

namespace SchoolManager.Management.Domain
{
    public interface IStudentMedicalProfileRepository : IRepository<StudentMedicalProfile>
    {
        Task<StudentMedicalProfile?> GetByStudentIdAsync(Guid studentId);
    }
}
