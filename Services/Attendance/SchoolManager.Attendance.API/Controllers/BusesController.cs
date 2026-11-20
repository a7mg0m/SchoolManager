using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Attendance.Application;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.API.Controllers
{
    [ApiController]
    [Route("api/bus")]
    public class BusesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BusesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllBusesQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get-active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetActiveBusesQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetBusByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        /// <summary>The buses driving one route</summary>
        [HttpGet("get-by-route/{busRouteId}")]
        public async Task<IActionResult> GetByRoute(Guid busRouteId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetBusesByRouteQuery(busRouteId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>New bus; the plate number must be unique and the route must exist</summary>
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateBusCommand command, CancellationToken cancellationToken)
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

        /// <summary>The capacity cannot drop below the bus's active assignments</summary>
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateBusCommand command, CancellationToken cancellationToken)
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
            var success = await _mediator.Send(new SetBusActiveStatusCommand(id, true), cancellationToken);

            if (!success)
                return NotFound();

            return Ok(new ApiResponse { message = "Activated Successfully" });
        }

        [HttpPut("deactivate/{id}")]
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new SetBusActiveStatusCommand(id, false), cancellationToken);

            if (!success)
                return NotFound();

            return Ok(new ApiResponse { message = "Deactivated Successfully" });
        }

        /// <summary>Not allowed while the bus has active assignments</summary>
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(new DeleteBusCommand(id), cancellationToken);

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
