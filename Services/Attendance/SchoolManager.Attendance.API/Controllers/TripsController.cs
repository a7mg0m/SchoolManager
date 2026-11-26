using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Attendance.Application;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.API.Controllers
{
    [ApiController]
    [Route("api/trip")]
    public class TripsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public TripsController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Admin / driver: plans a trip of a bus (one per bus, date and direction) with every student riding the bus
        /// on that date.
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateTripCommand command, CancellationToken cancellationToken)
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

        /// <summary>Driver: the bus leaves (planned → in progress)</summary>
        [HttpPut("start/{id}")]
        public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(new StartTripCommand(id), cancellationToken);

                if (!success)
                    return NotFound();

                return Ok(new ApiResponse { message = "Trip Started Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>Driver: the trip ended (in progress → completed)</summary>
        [HttpPut("complete/{id}")]
        public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(new CompleteTripCommand(id), cancellationToken);

                if (!success)
                    return NotFound();

                return Ok(new ApiResponse { message = "Trip Completed Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>Admin / driver: cancels a trip that is not completed</summary>
        [HttpPut("cancel/{id}")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(new CancelTripCommand(id), cancellationToken);

                if (!success)
                    return NotFound();

                return Ok(new ApiResponse { message = "Trip Cancelled Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>Driver: the bus's current position (latitude / longitude) while the trip runs</summary>
        [HttpPut("location/{id}")]
        public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateTripLocationCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(command with { Id = id }, cancellationToken);

                if (!success)
                    return NotFound();

                return Ok(new ApiResponse { message = "Location Updated Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>Driver: a student boarded, was dropped off or did not come (studentId + status in the body)</summary>
        [HttpPut("mark-student/{tripId}")]
        public async Task<IActionResult> MarkStudent(Guid tripId, [FromBody] MarkTripStudentCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(command with { TripId = tripId }, cancellationToken);

                if (!success)
                    return NotFound();

                return Ok(new ApiResponse { message = "Student Marked Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTripByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        /// <summary>
        /// Admin transport report: the trips between ?from= and ?to= ("yyyy-MM-dd", both included, at most a year), of
        /// one bus when ?busId= is given, with the students' marks. App users (parents / teachers / drivers) get 403.
        /// </summary>
        [Authorize]
        [HttpGet("report")]
        public async Task<IActionResult> GetReport([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] Guid? busId, CancellationToken cancellationToken)
        {
            if (_currentUser.ParentId is not null || _currentUser.TeacherId is not null || _currentUser.BusId is not null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "Forbidden"));

            var result = await _mediator.Send(new GetTripsReportQuery(from, to, busId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>The trips of a date ("yyyy-MM-dd"), of one bus when busId is given</summary>
        [HttpGet("get-by-date")]
        public async Task<IActionResult> GetByDate([FromQuery] DateOnly date, [FromQuery] Guid? busId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTripsByDateQuery(date, busId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>
        /// Parent app: today's running trip of one of the logged-in parent's children, to follow the bus (bus, driver,
        /// last position, the child's status). data is null when the child is on no running trip. The child must be
        /// the parent's (checked with the Management service); otherwise 404.
        /// </summary>
        /// <summary>
        /// Parent app: the trips of one of the logged-in parent's children between two dates ("yyyy-MM-dd", both
        /// included; default the last 30 days), newest first, with the bus and the child's status on each. The child
        /// must be the parent's (checked with the Management service); otherwise 404.
        /// </summary>
        [Authorize]
        [HttpGet("my-child/{studentId}/trips")]
        public async Task<IActionResult> GetMyChildTrips(Guid studentId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
        {
            if (_currentUser.ParentId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAParent"));

            var end = to ?? DateOnly.FromDateTime(DateTime.Today);
            var start = from ?? end.AddDays(-30);
            if (start > end)
                return Ok(new ApiResponse { status = 400, message = "End date cannot be before the start date." });

            try
            {
                var result = await _mediator.Send(
                    new GetMyChildTripsQuery(studentId, start, end, Request.Headers.Authorization.ToString()), cancellationToken);
                if (result is null)
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable"));

                return Ok(new ApiResponse { data = result });
            }
            catch (DomainException)
            {
                return NotFound(new ApiResponse(404, "StudentNotFound"));
            }
        }

        [Authorize]
        [HttpGet("my-child/{studentId}")]
        public async Task<IActionResult> GetMyChildCurrentTrip(Guid studentId, CancellationToken cancellationToken)
        {
            if (_currentUser.ParentId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAParent"));

            try
            {
                var result = await _mediator.Send(
                    new GetMyChildCurrentTripQuery(studentId, Request.Headers.Authorization.ToString()), cancellationToken);
                return Ok(new ApiResponse { data = result });
            }
            catch (DomainException)
            {
                return NotFound(new ApiResponse(404, "StudentNotFound"));
            }
        }
    }
}
