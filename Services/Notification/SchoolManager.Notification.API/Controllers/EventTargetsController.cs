using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.API.Controllers
{
    [Route("api/event-target")]
    public class EventTargetsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public EventTargetsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllEventTargetsQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEventTargetByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get-by-event/{eventId}")]
        public async Task<IActionResult> GetByEventId(Guid eventId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEventTargetsByEventIdQuery(eventId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }
    }
}
