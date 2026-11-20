using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Attendance.Application;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.API.Controllers
{
    [ApiController]
    [Route("api/bus-assignment")]
    public class BusAssignmentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BusAssignmentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Admin: assigns students to a bus for an academic year with the bus fee entered here (per student, not the
        /// grade fees of the year). Each assignment is sent to Financial to become a Transportation student fee.
        /// </summary>
        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] AssignStudentsToBusCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var ids = await _mediator.Send(command, cancellationToken);

                return Ok(new ApiResponse
                {
                    data = ids,
                    message = ids.Count == 1 ? "1 student assigned to the bus" : $"{ids.Count} students assigned to the bus"
                });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>Admin: new bus fee of the student (Financial is told)</summary>
        [HttpPut("change-fee/{id}")]
        public async Task<IActionResult> ChangeFee(Guid id, [FromBody] ChangeBusFeeCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(command with { Id = id }, cancellationToken);

                if (!success)
                    return NotFound();

                return Ok(new ApiResponse { message = "Fee Changed Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>Admin: moves the student to another bus / stop (the new bus must have a free seat)</summary>
        [HttpPut("move/{id}")]
        public async Task<IActionResult> Move(Guid id, [FromBody] MoveStudentToBusCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(command with { Id = id }, cancellationToken);

                if (!success)
                    return NotFound();

                return Ok(new ApiResponse { message = "Moved Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>Admin: the student stops riding the bus after the end date (Financial is told)</summary>
        [HttpPut("end/{id}")]
        public async Task<IActionResult> End(Guid id, [FromBody] EndBusAssignmentCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(command with { Id = id }, cancellationToken);

                if (!success)
                    return NotFound();

                return Ok(new ApiResponse { message = "Ended Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetBusAssignmentByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        /// <summary>The students of a bus (of one academic year when given)</summary>
        [HttpGet("get-by-bus/{busId}")]
        public async Task<IActionResult> GetByBus(Guid busId, [FromQuery] Guid? academicYearId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAssignmentsByBusQuery(busId, academicYearId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>A student's bus assignments, newest first</summary>
        [HttpGet("get-by-student/{studentId}")]
        public async Task<IActionResult> GetByStudent(Guid studentId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAssignmentsByStudentQuery(studentId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }
    }
}
