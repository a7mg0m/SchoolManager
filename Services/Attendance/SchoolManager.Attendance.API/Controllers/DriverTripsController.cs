using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Attendance.Application;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.API.Controllers
{
    /// <summary>
    /// Driver app: the logged-in driver's bus and its trips. The bus comes from the bus_id claim of the token
    /// (drivers only: others get 403 NotADriver); a trip of another bus is 404.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/trip/my-bus")]
    public class DriverTripsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public DriverTripsController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        /// <summary>The driver's bus with its route (name and stops)</summary>
        [HttpGet]
        public async Task<IActionResult> GetMyBus(CancellationToken cancellationToken)
        {
            if (_currentUser.BusId is not Guid busId)
                return NotADriver();

            var bus = await _mediator.Send(new GetBusByIdQuery(busId), cancellationToken);
            if (bus is null)
                return NotFound(new ApiResponse(404, "BusNotFound"));

            var route = await _mediator.Send(new GetBusRouteByIdQuery(bus.BusRouteId), cancellationToken);
            return Ok(new ApiResponse { data = new { bus, route } });
        }

        /// <summary>The bus's trips between two dates ("yyyy-MM-dd", both included; default the last 14 days and the next 7), newest first</summary>
        [HttpGet("trips")]
        public async Task<IActionResult> GetTrips([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
        {
            if (_currentUser.BusId is not Guid busId)
                return NotADriver();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var start = from ?? today.AddDays(-14);
            var end = to ?? today.AddDays(7);
            if (start > end)
                return Ok(new ApiResponse { status = 400, message = "End date cannot be before the start date." });

            var result = await _mediator.Send(new GetBusTripsQuery(busId, start, end), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>One trip of the bus, with its students</summary>
        [HttpGet("trips/{id}")]
        public async Task<IActionResult> GetTrip(Guid id, CancellationToken cancellationToken)
        {
            if (_currentUser.BusId is not Guid busId)
                return NotADriver();

            var trip = await _mediator.Send(new GetTripByIdQuery(id), cancellationToken);
            if (trip is null || trip.BusId != busId)
                return NotFound(new ApiResponse(404, "TripNotFound"));

            return Ok(new ApiResponse { data = trip });
        }

        /// <summary>
        /// The trip's students with their status, stop, class and parent's phone, in stop order
        /// (names are empty when the Management service does not answer)
        /// </summary>
        [HttpGet("trips/{id}/students")]
        public async Task<IActionResult> GetTripStudents(Guid id, CancellationToken cancellationToken)
        {
            if (_currentUser.BusId is not Guid busId)
                return NotADriver();

            var result = await _mediator.Send(
                new GetDriverTripStudentsQuery(id, busId, Request.Headers.Authorization.ToString()), cancellationToken);
            if (result is null)
                return NotFound(new ApiResponse(404, "TripNotFound"));

            return Ok(new ApiResponse { data = result });
        }

        /// <summary>
        /// A student boarded the bus, arrived (dropped off at school or at home) or did not come
        /// (body: { "status": 1 = Boarded, 2 = DroppedOff, 3 = Absent }). The trip must be running.
        /// </summary>
        [HttpPut("trips/{id}/students/{studentId}")]
        public Task<IActionResult> MarkStudent(Guid id, Guid studentId, [FromBody] MarkMyTripStudentRequest request, CancellationToken cancellationToken)
            => ChangeAsync(id, new MarkTripStudentCommand(id, studentId, request.Status), "Student Marked Successfully", cancellationToken);

        /// <summary>
        /// The bus's current position, sent automatically by the driver app while the trip runs
        /// (parents follow it on the map)
        /// </summary>
        [HttpPut("trips/{id}/location")]
        public Task<IActionResult> UpdateLocation(Guid id, [FromBody] MyTripLocationRequest request, CancellationToken cancellationToken)
            => ChangeAsync(id, new UpdateTripLocationCommand(id, request.Latitude, request.Longitude), "Location Updated Successfully", cancellationToken);

        /// <summary>Plans a trip of the driver's bus (date + type); every student riding the bus that day is added</summary>
        [HttpPost("trips")]
        public async Task<IActionResult> Create([FromBody] CreateMyTripRequest request, CancellationToken cancellationToken)
        {
            if (_currentUser.BusId is not Guid busId)
                return NotADriver();

            try
            {
                var id = await _mediator.Send(new CreateTripCommand(busId, request.Date, request.Type), cancellationToken);
                return Ok(new ApiResponse { InsertedId = id.ToString() });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>The bus leaves (planned → in progress)</summary>
        [HttpPut("trips/{id}/start")]
        public Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
            => ChangeAsync(id, new StartTripCommand(id), "Trip Started Successfully", cancellationToken);

        /// <summary>The trip ended (in progress → completed)</summary>
        [HttpPut("trips/{id}/complete")]
        public Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
            => ChangeAsync(id, new CompleteTripCommand(id), "Trip Completed Successfully", cancellationToken);

        /// <summary>Cancels a trip that is not completed</summary>
        [HttpPut("trips/{id}/cancel")]
        public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
            => ChangeAsync(id, new CancelTripCommand(id), "Trip Cancelled Successfully", cancellationToken);

        /// <summary>Runs a command on a trip after checking it belongs to the driver's bus</summary>
        private async Task<IActionResult> ChangeAsync(Guid id, IRequest<bool> command, string message, CancellationToken cancellationToken)
        {
            if (_currentUser.BusId is not Guid busId)
                return NotADriver();

            var trip = await _mediator.Send(new GetTripByIdQuery(id), cancellationToken);
            if (trip is null || trip.BusId != busId)
                return NotFound(new ApiResponse(404, "TripNotFound"));

            try
            {
                var success = await _mediator.Send(command, cancellationToken);
                if (!success)
                    return NotFound(new ApiResponse(404, "TripNotFound"));

                return Ok(new ApiResponse { message = message });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        private IActionResult NotADriver()
            => StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotADriver"));
    }

    /// <summary>Body of POST api/trip/my-bus/trips</summary>
    public record CreateMyTripRequest(DateOnly Date, TripType Type);

    /// <summary>Body of PUT api/trip/my-bus/trips/{id}/students/{studentId}</summary>
    public record MarkMyTripStudentRequest(TripStudentStatus Status);

    /// <summary>Body of PUT api/trip/my-bus/trips/{id}/location</summary>
    public record MyTripLocationRequest(double Latitude, double Longitude);
}
