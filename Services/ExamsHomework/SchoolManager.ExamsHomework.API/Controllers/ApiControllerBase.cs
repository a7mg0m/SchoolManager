using Microsoft.AspNetCore.Mvc;

namespace SchoolManager.ExamsHomework.API.Controllers
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected Guid CurrentUserId =>
            Request.Headers.TryGetValue("X-User-Id", out var value) && Guid.TryParse(value, out var id)
                ? id
                : Guid.Empty;
    }
}
