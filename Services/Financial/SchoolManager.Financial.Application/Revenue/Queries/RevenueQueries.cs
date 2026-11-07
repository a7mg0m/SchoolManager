using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record GetRevenueByIdQuery(Guid Id) : IQuery<RevenueDto?>;

    public record GetAllRevenuesQuery() : IQuery<List<RevenueDto>>;
}
