using BuildingBlocks;

namespace SchoolManager.Management.Domain
{
    public interface IClinicVisitRepository : IRepository<ClinicVisit>
    {
        Task<ClinicVisit?> GetByIdWithMedicationsAsync(Guid id);
        Task<List<ClinicVisit>> GetByStudentIdAsync(Guid studentId);
    }
}
