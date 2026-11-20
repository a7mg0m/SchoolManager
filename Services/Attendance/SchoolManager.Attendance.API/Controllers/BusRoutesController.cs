using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Attendance.Application;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.API.Controllers
{
    [ApiController]
    [Route("api/bus-route")]
    public class BusRoutesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BusRoutesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllBusRoutesQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get-active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetActiveBusRoutesQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetBusRouteByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateBusRouteCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var id = await _mediator.Send(command, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>Updates the route; the stops sent replace the saved ones</summary>
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateBusRouteCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(command, cancellationToken);

                if (!success)
                    return Ok(new ApiResponse { status = -1, message = "Update Failed" });

                return Ok(new ApiResponse { message = "Updated Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        [HttpPut("activate/{id}")]
        public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new SetBusRouteActiveStatusCommand(id, true), cancellationToken);

            if (!success)
                return NotFound();

            return Ok(new ApiResponse { message = "Activated Successfully" });
        }

        [HttpPut("deactivate/{id}")]
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new SetBusRouteActiveStatusCommand(id, false), cancellationToken);

            if (!success)
                return NotFound();

            return Ok(new ApiResponse { message = "Deactivated Successfully" });
        }

        /// <summary>Not allowed while buses use the route</summary>
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(new DeleteBusRouteCommand(id), cancellationToken);

                if (!success)
                    return NotFound();

                return Ok(new ApiResponse { message = "Deleted Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }
    }
}
