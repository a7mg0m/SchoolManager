using BuildingBlocks.CQRS;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Application
{
    public class GetMenuTreeQueryHandler : IQueryHandler<GetMenuTreeQuery, List<MenuItemTreeDto>>
    {
        private readonly IMenuItemRepository _repository;

        public GetMenuTreeQueryHandler(IMenuItemRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<MenuItemTreeDto>> Handle(GetMenuTreeQuery query, CancellationToken cancellationToken)
        {
            var all = await _repository.GetAllOrderedAsync();
            return MenuItemMapper.BuildTree(all, query.ActiveOnly);
        }
    }
}
