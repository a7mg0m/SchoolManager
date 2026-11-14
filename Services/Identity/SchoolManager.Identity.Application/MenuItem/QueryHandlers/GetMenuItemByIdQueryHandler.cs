using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class GetMenuItemByIdQueryHandler : IQueryHandler<GetMenuItemByIdQuery, MenuItemDto?>
    {
        private readonly IMenuItemRepository _repository;

        public GetMenuItemByIdQueryHandler(IMenuItemRepository repository)
        {
            _repository = repository;
        }

        public async Task<MenuItemDto?> Handle(GetMenuItemByIdQuery query, CancellationToken cancellationToken)
        {
            // Load all so the parent name and children count can be filled (menus are small)
            var all = await _repository.GetAllOrderedAsync();
            var item = all.FirstOrDefault(m => m.Id == query.Id);
            return item is null ? null : MenuItemMapper.ToDtos(all, [item]).Single();
        }
    }
}
