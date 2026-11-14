using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class GetAllMenuItemsQueryHandler : IQueryHandler<GetAllMenuItemsQuery, List<MenuItemDto>>
    {
        private readonly IMenuItemRepository _repository;

        public GetAllMenuItemsQueryHandler(IMenuItemRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<MenuItemDto>> Handle(GetAllMenuItemsQuery query, CancellationToken cancellationToken)
        {
            var all = await _repository.GetAllOrderedAsync();
            return MenuItemMapper.ToDtos(all);
        }
    }
}
