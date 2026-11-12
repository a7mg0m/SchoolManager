using BuildingBlocks;
using MediatR;
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

        public StudentFeesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllStudentFeesQuery(), cancellationToken);
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
        public async Task<IActionResult> GetByStudent(Guid studentId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetStudentFeesByStudentQuery(studentId), cancellationToken);
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
                    new AssignStudentFeesCommand(dto.StudentIds, dto.FeeIds, dto.DiscountAmount, dto.DueDate),
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
