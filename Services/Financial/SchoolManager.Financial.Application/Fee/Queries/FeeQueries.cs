using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record GetFeeByIdQuery(Guid Id) : IQuery<FeeDto?>;

    /// <param name="AcademicYearId">Only that year's fees (and those saved without a year); null = all</param>
    public record GetAllFeesQuery(Guid? AcademicYearId = null) : IQuery<List<FeeDto>>;
}
