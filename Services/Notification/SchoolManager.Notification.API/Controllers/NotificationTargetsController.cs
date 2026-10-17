using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.API.Controllers
{
    [Route("api/notification-target")]
    public class NotificationTargetsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationTargetsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllNotificationTargetsQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetNotificationTargetByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get-by-notification/{notificationId}")]
        public async Task<IActionResult> GetByNotificationId(Guid notificationId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetNotificationTargetsByNotificationIdQuery(notificationId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }
    }
}
