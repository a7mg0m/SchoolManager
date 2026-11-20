using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record GetRevenueByIdQuery(Guid Id) : IQuery<RevenueDto?>;

    /// <param name="AcademicYearId">Only that year's revenues; null = all</param>
    public record GetAllRevenuesQuery(Guid? AcademicYearId = null) : IQuery<List<RevenueDto>>;
}
