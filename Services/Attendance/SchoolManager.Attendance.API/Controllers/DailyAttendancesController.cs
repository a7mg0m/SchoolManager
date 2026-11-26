using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Attendance.Application;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.API.Controllers
{
    [ApiController]
    [Route("api/daily-attendance")]
    public class DailyAttendancesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public DailyAttendancesController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Teacher app: the logged-in teacher records one lesson for the students of a section. Each student's day
        /// is created with the first lesson recorded; sending the same lesson again updates it.
        /// </summary>
        [Authorize]
        [HttpPost("record-period")]
        public async Task<IActionResult> RecordPeriod([FromBody] RecordPeriodAttendanceCommand command, CancellationToken cancellationToken)
        {
            if (_currentUser.TeacherId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            try
            {
                await _mediator.Send(command, cancellationToken);
                return Ok(new ApiResponse { message = "Attendance Recorded Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// Admin dashboard: a school employee creates or updates a student's day (status, check-out time, notes).
        /// </summary>
        [Authorize]
        [HttpPost("set-day")]
        public async Task<IActionResult> SetDay([FromBody] SetDailyAttendanceCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var id = await _mediator.Send(command, cancellationToken);
                return Ok(new ApiResponse { InsertedId = id.ToString(), message = "Saved Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// The student left school at <paramref name="time"/> ("HH:mm"); not before the check-in time.
        /// </summary>
        [Authorize]
        [HttpPut("check-out/{id}")]
        public async Task<IActionResult> CheckOut(Guid id, [FromQuery] TimeOnly time, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(new CheckOutStudentCommand(id, time), cancellationToken);

                if (!success)
                    return NotFound();

                return Ok(new ApiResponse { message = "Checked Out Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new DeleteDailyAttendanceCommand(id), cancellationToken);

            if (!success)
                return NotFound();

            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetDailyAttendanceByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        /// <summary>
        /// Admin attendance report: every recorded school day between ?from= and ?to= ("yyyy-MM-dd", both included,
        /// at most a year), optionally of ?academicYearId= / ?gradeId= / ?sectionId=, with each day's lesson counts.
        /// App users (parents / teachers / drivers) get 403.
        /// </summary>
        [Authorize]
        [HttpGet("report")]
        public async Task<IActionResult> GetReport(
            [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] Guid? academicYearId, [FromQuery] Guid? gradeId,
            [FromQuery] Guid? sectionId, CancellationToken cancellationToken)
        {
            if (_currentUser.ParentId is not null || _currentUser.TeacherId is not null || _currentUser.BusId is not null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "Forbidden"));

            var result = await _mediator.Send(new GetAttendanceReportQuery(from, to, academicYearId, gradeId, sectionId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>Admin dashboard: the days of a section's students on one date ("yyyy-MM-dd")</summary>
        [HttpGet("get-by-section/{sectionId}")]
        public async Task<IActionResult> GetBySection(Guid sectionId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSectionAttendanceByDateQuery(sectionId, date), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>Teacher app: what was recorded for one lesson (timetable slot) on a date, one row per student</summary>
        [Authorize]
        [HttpGet("get-by-period/{timetableSlotId}")]
        public async Task<IActionResult> GetByPeriod(Guid timetableSlotId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
        {
            if (_currentUser.TeacherId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            var result = await _mediator.Send(new GetPeriodAttendanceQuery(timetableSlotId, date), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>A student's days between two dates ("yyyy-MM-dd", both included)</summary>
        [HttpGet("get-by-student/{studentId}")]
        public async Task<IActionResult> GetByStudent(Guid studentId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetStudentAttendanceQuery(studentId, from, to), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>
        /// Parent app: attendance of one of the logged-in parent's children between two dates. The child must be the
        /// parent's (checked with the Management service); otherwise 404.
        /// </summary>
        [Authorize]
        [HttpGet("my-child/{studentId}")]
        public async Task<IActionResult> GetMyChild(Guid studentId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken)
        {
            if (_currentUser.ParentId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAParent"));

            try
            {
                var result = await _mediator.Send(
                    new GetMyChildAttendanceQuery(studentId, from, to, Request.Headers.Authorization.ToString()), cancellationToken);
                if (result is null)
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable"));

                return Ok(new ApiResponse { data = result });
            }
            catch (DomainException)
            {
                return NotFound(new ApiResponse(404, "StudentNotFound"));
            }
        }
        /// <summary>
        /// Teacher app: the school days of a student the logged-in teacher teaches, between two dates ("yyyy-MM-dd", both
        /// included). Checked with the Management service; a student the teacher does not teach: 404.
        /// </summary>
        [Authorize]
        [HttpGet("my-student/{studentId}")]
        public async Task<IActionResult> GetMyStudent(
            Guid studentId, [FromQuery] DateOnly from, [FromQuery] DateOnly to,
            [FromServices] ITeacherStudentsService teacherStudents, CancellationToken cancellationToken)
        {
            if (_currentUser.TeacherId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            var teaches = await teacherStudents.TeachesStudentAsync(Request.Headers.Authorization.ToString(), studentId, cancellationToken);
            if (teaches is null)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable"));
            if (teaches == false)
                return NotFound(new ApiResponse(404, "StudentNotTaught"));

            var result = await _mediator.Send(new GetStudentAttendanceQuery(studentId, from, to), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }
    }
}
