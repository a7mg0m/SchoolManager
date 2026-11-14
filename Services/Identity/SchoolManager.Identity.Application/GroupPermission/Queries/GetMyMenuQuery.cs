using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    /// <summary>Menu of the logged-in user (from the JWT), filtered by the permissions of their employee group.</summary>
    public record GetMyMenuQuery() : IQuery<MyMenuDto>;
}
