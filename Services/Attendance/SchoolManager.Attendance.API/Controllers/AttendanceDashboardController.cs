using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Attendance.Application;

namespace SchoolManager.Attendance.API.Controllers
{
    [ApiController]
    [Route("api/attendance-dashboard")]
    public class AttendanceDashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AttendanceDashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Admin home page: attendance of a day ("yyyy-MM-dd", default today) and the transport figures
        /// (buses, seats, riders, the day's trips, drivers without a login) for an academic year (null = every year).
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] DateOnly? date, [FromQuery] Guid? academicYearId, CancellationToken cancellationToken)
        {
            var day = date ?? DateOnly.FromDateTime(DateTime.Now);
            var result = await _mediator.Send(new GetDashboardSummaryQuery(day, academicYearId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }
    }
}
