using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.API.Controllers
{
    [Route("api/notification")]
    public class NotificationsController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IManagementClient _management;

        public NotificationsController(IMediator mediator, IManagementClient management)
        {
            _mediator = mediator;
            _management = management;
        }

        /// <summary>
        /// Notifications already sent that concern one of the logged-in parent's children (parent app): for the whole
        /// school, its grade or class, or the child itself. The child must be the parent's (checked with Management).
        /// </summary>
        [Authorize]
        [HttpGet("my-child/{studentId}")]
        public async Task<IActionResult> GetMyChildNotifications(Guid studentId, [FromQuery] Guid? academicYearId, CancellationToken cancellationToken)
        {
            if (CurrentParentId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAParent"));

            var children = await _management.GetMyChildrenAsync(Request.Headers.Authorization.ToString(), cancellationToken);
            if (children is null)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable"));

            var child = children.FirstOrDefault(c => c.Id == studentId);
            if (child is null)
                return NotFound(new ApiResponse(404, "StudentNotFound"));

            var result = await _mediator.Send(new GetChildNotificationsQuery(child, academicYearId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>Notifications already sent for the whole school or for the logged-in teacher (teacher app).</summary>
        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyNotifications([FromQuery] Guid? academicYearId, CancellationToken cancellationToken)
        {
            if (CurrentTeacherId is not Guid teacherId)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            var result = await _mediator.Send(new GetTeacherNotificationsQuery(teacherId, academicYearId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>Notifications already sent for the whole school or for the logged-in driver's bus (driver app).</summary>
        [Authorize]
        [HttpGet("my-driver")]
        public async Task<IActionResult> GetMyDriverNotifications([FromQuery] Guid? academicYearId, CancellationToken cancellationToken)
        {
            if (CurrentBusId is not Guid busId)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotADriver"));

            var result = await _mediator.Send(new GetDriverNotificationsQuery(busId, academicYearId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>Notifications of the academic year chosen in the admin header (null: all), newest first.</summary>
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll([FromQuery] Guid? academicYearId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllNotificationsQuery(academicYearId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetNotificationByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto, CancellationToken cancellationToken)
        {
            var command = new CreateNotificationCommand(dto.Title, dto.Description, dto.SentOn,
                dto.NotificationType, dto.ImageUrl, dto.Targets, dto.AcademicYearId);
            var id = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse { InsertedId = id.ToString() });
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateNotificationCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse { message = "Updated Successfully" });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteNotificationCommand(id), cancellationToken);
            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
