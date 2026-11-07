using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Financial.Application;

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

        [HttpPut("apply-payment/{id}")]
        public async Task<IActionResult> ApplyPayment(Guid id, [FromBody] ApplyStudentFeePaymentDto dto, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new ApplyStudentFeePaymentCommand(id, dto.Amount, dto.Currency), cancellationToken);

            if (!success)
                return Ok(new ApiResponse { status = -1, message = "Apply Payment Failed" });

            return Ok(new ApiResponse { message = "Payment Applied Successfully" });
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
