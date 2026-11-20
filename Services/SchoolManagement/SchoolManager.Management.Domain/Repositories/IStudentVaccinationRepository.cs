using BuildingBlocks;

namespace SchoolManager.Management.Domain
{
    public interface IStudentVaccinationRepository : IRepository<StudentVaccination>
    {
        Task<List<StudentVaccination>> GetByStudentIdAsync(Guid studentId);
    }
}
