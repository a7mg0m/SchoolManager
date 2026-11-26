using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Financial.Application;
using SchoolManager.Financial.Domain;

namespace SchoolManager.Financial.API.Controllers
{
    [ApiController]
    [Route("api/student-fee")]
    public class StudentFeesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly IParentChildrenService _parentChildren;

        public StudentFeesController(IMediator mediator, ICurrentUserService currentUser, IParentChildrenService parentChildren)
        {
            _mediator = mediator;
            _currentUser = currentUser;
            _parentChildren = parentChildren;
        }

        /// <summary>
        /// Fees of one of the logged-in parent's children (parent app). The child must be in the parent's
        /// children (checked with the Management service); otherwise 404. Newest due date first.
        /// </summary>
        [Authorize]
        [HttpGet("my-child/{studentId}")]
        public async Task<IActionResult> GetMyChildFees(Guid studentId, [FromQuery] Guid? academicYearId, CancellationToken cancellationToken)
        {
            if (_currentUser.ParentId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAParent"));

            var children = await _parentChildren.GetMyChildrenIdsAsync(Request.Headers.Authorization.ToString(), cancellationToken);
            if (children is null)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable"));
            if (!children.Contains(studentId))
                return NotFound(new ApiResponse(404, "StudentNotFound"));

            // The academic year chosen in the parent app (null = every year)
            var fees = await _mediator.Send(new GetStudentFeesByStudentQuery(studentId, academicYearId), cancellationToken);
            return Ok(new ApiResponse { data = fees.OrderByDescending(f => f.DueDate).ToList() });
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll([FromQuery] Guid? academicYearId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllStudentFeesQuery(academicYearId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetStudentFeeByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get-by-student/{studentId}")]
        public async Task<IActionResult> GetByStudent(Guid studentId, [FromQuery] Guid? academicYearId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetStudentFeesByStudentQuery(studentId, academicYearId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>
        /// Assigns the selected fees to one or more students (one StudentFee per student).
        /// </summary>
        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] AssignStudentFeesDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var ids = await _mediator.Send(
                    new AssignStudentFeesCommand(dto.StudentIds, dto.FeeIds, dto.DiscountAmount, dto.DueDate, dto.AcademicYearId),
                    cancellationToken);

                return Ok(new ApiResponse
                {
                    data = ids,
                    message = ids.Count == 1 ? "Fees assigned to 1 student" : $"Fees assigned to {ids.Count} students"
                });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// Payments applied to student fees between two dates (?from=&to= "yyyy-MM-dd", both included, either open),
        /// each with whether it is a bus fee (income report).
        /// </summary>
        [HttpGet("payments")]
        public async Task<IActionResult> GetPayments([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetStudentFeePaymentsQuery(from, to), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpPut("apply-payment/{id}")]
        public async Task<IActionResult> ApplyPayment(Guid id, [FromBody] ApplyStudentFeePaymentDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(new ApplyStudentFeePaymentCommand(id, dto.Amount, dto.Currency), cancellationToken);

                if (!success)
                    return Ok(new ApiResponse { status = -1, message = "Apply Payment Failed" });

                return Ok(new ApiResponse { message = "Payment Applied Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// Forgives the remaining balance of a fee (payments already made are kept).
        /// </summary>
        [HttpPut("waive/{id}")]
        public async Task<IActionResult> Waive(Guid id, [FromBody] WaiveStudentFeeDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(new WaiveStudentFeeCommand(id, dto.Reason), cancellationToken);

                if (!success)
                    return NotFound();

                return Ok(new ApiResponse { message = "Fee Waived Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// Undoes a waiver; the waived balance becomes payable again.
        /// </summary>
        [HttpPut("restore-waiver/{id}")]
        public async Task<IActionResult> RestoreWaiver(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(new RestoreStudentFeeWaiverCommand(id), cancellationToken);

                if (!success)
                    return NotFound();

                return Ok(new ApiResponse { message = "Waiver Restored Successfully" });
            }
            catch (DomainException ex)
            {
                return Ok(new ApiResponse { status = 400, message = ex.Message });
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new DeleteStudentFeeCommand(id), cancellationToken);

            if (!success)
                return NotFound();

            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
