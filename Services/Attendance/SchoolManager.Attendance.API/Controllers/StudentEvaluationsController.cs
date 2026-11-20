using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Attendance.Application;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.API.Controllers
{
    [ApiController]
    [Route("api/student-evaluation")]
    public class StudentEvaluationsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public StudentEvaluationsController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Teacher app: the logged-in teacher evaluates a student in a lesson (focus, interaction, behavior, health).
        /// Evaluating the same student, lesson and day again updates it; the parent's pending request(s) are answered.
        /// </summary>
        [Authorize]
        [HttpPost("evaluate")]
        public async Task<IActionResult> Evaluate([FromBody] EvaluateStudentCommand command, CancellationToken cancellationToken)
        {
            if (_currentUser.TeacherId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

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

        /// <summary>Teacher app: changes an evaluation (only the teacher who made it)</summary>
        [Authorize]
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateStudentEvaluationCommand command, CancellationToken cancellationToken)
        {
            if (_currentUser.TeacherId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

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

        /// <summary>A teacher deletes only their own evaluations (the school office may delete any)</summary>
        [Authorize]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(new DeleteStudentEvaluationCommand(id), cancellationToken);

                if (!success)
                    return NotFound();

                return Ok(new ApiResponse { message = "Deleted Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetStudentEvaluationByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        /// <summary>The evaluations of one lesson (timetable slot) on a date ("yyyy-MM-dd")</summary>
        [HttpGet("get-by-period/{timetableSlotId}")]
        public async Task<IActionResult> GetByPeriod(Guid timetableSlotId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEvaluationsBySlotQuery(timetableSlotId, date), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>A student's evaluations between two dates ("yyyy-MM-dd", both included)</summary>
        [HttpGet("get-by-student/{studentId}")]
        public async Task<IActionResult> GetByStudent(Guid studentId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetStudentEvaluationsQuery(studentId, from, to), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>
        /// Parent app: the evaluations of one of the logged-in parent's children between two dates. The child must be
        /// the parent's (checked with the Management service); otherwise 404.
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
                    new GetMyChildEvaluationsQuery(studentId, from, to, Request.Headers.Authorization.ToString()), cancellationToken);
                if (result is null)
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable"));

                return Ok(new ApiResponse { data = result });
            }
            catch (DomainException)
            {
                return NotFound(new ApiResponse(404, "StudentNotFound"));
            }
        }
    }
}
