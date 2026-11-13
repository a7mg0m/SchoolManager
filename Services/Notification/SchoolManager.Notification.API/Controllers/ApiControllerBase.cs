using BuildingBlocks;
using Microsoft.AspNetCore.Mvc;

namespace SchoolManager.Notification.API.Controllers
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>The logged-in user (from the validated JWT), or Guid.Empty when anonymous.</summary>
        protected Guid CurrentUserId =>
            HttpContext.RequestServices.GetRequiredService<ICurrentUserService>().UserId;
    }
}
