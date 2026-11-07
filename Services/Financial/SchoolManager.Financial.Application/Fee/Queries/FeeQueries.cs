using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record GetFeeByIdQuery(Guid Id) : IQuery<FeeDto?>;

    public record GetAllFeesQuery() : IQuery<List<FeeDto>>;
}
