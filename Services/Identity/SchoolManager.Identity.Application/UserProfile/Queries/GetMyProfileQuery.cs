using BuildingBlocks.CQRS;

namespace SchoolManager.Identity.Application
{
    /// <summary>Profile of the logged-in user (from the JWT).</summary>
    public record GetMyProfileQuery() : IQuery<MyProfileDto>;
}
