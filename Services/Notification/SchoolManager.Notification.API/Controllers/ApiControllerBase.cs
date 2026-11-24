using BuildingBlocks;
using Microsoft.AspNetCore.Mvc;

namespace SchoolManager.Notification.API.Controllers
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>The logged-in user (from the validated JWT), or Guid.Empty when anonymous.</summary>
        protected Guid CurrentUserId => CurrentUser.UserId;

        /// <summary>Employee id of the logged-in user, or null when the user is not an employee.</summary>
        protected Guid? CurrentEmployeeId => CurrentUser.EmployeeId;

        /// <summary>Teacher id of the logged-in user, or null when the user is not a teacher.</summary>
        protected Guid? CurrentTeacherId => CurrentUser.TeacherId;

        /// <summary>Parent id of the logged-in user, or null when the user is not a parent.</summary>
        protected Guid? CurrentParentId => CurrentUser.ParentId;

        /// <summary>Bus id of the logged-in driver, or null when the user is not a driver.</summary>
        protected Guid? CurrentBusId => CurrentUser.BusId;

        private ICurrentUserService CurrentUser =>
            HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
    }
}
