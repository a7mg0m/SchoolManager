using BuildingBlocks.CQRS;

namespace SchoolManager.Financial.Application
{
    public record GetRevenueCategoryByIdQuery(Guid Id) : IQuery<RevenueCategoryDto?>;

    public record GetAllRevenueCategoriesQuery() : IQuery<List<RevenueCategoryDto>>;
}
