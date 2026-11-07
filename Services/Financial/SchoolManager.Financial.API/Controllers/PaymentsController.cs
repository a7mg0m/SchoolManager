using AutoMapper;
using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Financial.Application;

namespace SchoolManager.Financial.API.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public PaymentsController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllPaymentsQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPaymentByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get-by-student-fee/{studentFeeId}")]
        public async Task<IActionResult> GetByStudentFee(Guid studentFeeId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPaymentsByStudentFeeQuery(studentFeeId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto, CancellationToken cancellationToken)
        {
            var command = _mapper.Map<CreatePaymentCommand>(dto);
            var id = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
        }

        [HttpPut("complete/{id}")]
        public async Task<IActionResult> Complete(Guid id, [FromQuery] string transactionReference, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new CompletePaymentCommand(id, transactionReference), cancellationToken);

            if (!success)
                return Ok(new ApiResponse { status = -1, message = "Complete Failed" });

            return Ok(new ApiResponse { message = "Payment Completed Successfully" });
        }

        [HttpPut("fail/{id}")]
        public async Task<IActionResult> Fail(Guid id, [FromQuery] string reason, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new FailPaymentCommand(id, reason), cancellationToken);

            if (!success)
                return Ok(new ApiResponse { status = -1, message = "Fail Failed" });

            return Ok(new ApiResponse { message = "Payment Marked As Failed" });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new DeletePaymentCommand(id), cancellationToken);

            if (!success)
                return NotFound();

            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
