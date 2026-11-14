using AutoMapper;

namespace SchoolManager.Identity.Application
{
    public class MenuItemMappingProfile : Profile
    {
        public MenuItemMappingProfile()
        {
            CreateMap<CreateMenuItemDto, CreateMenuItemCommand>()
                .ConstructUsing(src => new CreateMenuItemCommand(
                    src.NameAr,
                    src.NameEn,
                    src.Icon,
                    src.Route,
                    src.Order,
                    src.ParentId,
                    src.IsActive));

            CreateMap<UpdateMenuItemDto, UpdateMenuItemCommand>()
                .ConstructUsing(src => new UpdateMenuItemCommand(
                    src.Id,
                    src.NameAr,
                    src.NameEn,
                    src.Icon,
                    src.Route,
                    src.Order,
                    src.ParentId));
        }
    }
}
