using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Attendance.Application;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.API.Controllers
{
    [ApiController]
    [Route("api/evaluation-request")]
    public class EvaluationRequestsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public EvaluationRequestsController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Parent app: the logged-in parent asks a teacher to evaluate their child (the child must be theirs;
        /// one pending request per student and teacher).
        /// </summary>
        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateEvaluationRequestCommand command, CancellationToken cancellationToken)
        {
            if (_currentUser.ParentId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAParent"));

            try
            {
                // The parent's token, to ask Management for the parent's children
                var id = await _mediator.Send(command with { Authorization = Request.Headers.Authorization.ToString() }, cancellationToken);
                return Ok(new ApiResponse { InsertedId = id.ToString(), message = "Request Sent Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>Parent app: cancels a pending request (only the parent who made it)</summary>
        [Authorize]
        [HttpPut("cancel/{id}")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
        {
            if (_currentUser.ParentId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAParent"));

            try
            {
                var success = await _mediator.Send(new CancelEvaluationRequestCommand(id), cancellationToken);

                if (!success)
                    return NotFound();

                return Ok(new ApiResponse { message = "Request Cancelled Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>Teacher app: the pending evaluation requests to the logged-in teacher, oldest first</summary>
        [Authorize]
        [HttpGet("my-pending")]
        public async Task<IActionResult> GetMyPending(CancellationToken cancellationToken)
        {
            if (_currentUser.TeacherId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            var result = await _mediator.Send(new GetMyPendingEvaluationRequestsQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>Parent app: the logged-in parent's requests (of one child when studentId is given), with their status</summary>
        [Authorize]
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests([FromQuery] Guid? studentId, CancellationToken cancellationToken)
        {
            if (_currentUser.ParentId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAParent"));

            var result = await _mediator.Send(new GetMyEvaluationRequestsQuery(studentId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }
    }
}
