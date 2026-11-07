using AutoMapper;
using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Financial.Application;

namespace SchoolManager.Financial.API.Controllers
{
    [ApiController]
    [Route("api/payment-method")]
    public class PaymentMethodsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public PaymentMethodsController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllPaymentMethodsQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPaymentMethodByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreatePaymentMethodDto dto, CancellationToken cancellationToken)
        {
            var command = _mapper.Map<CreatePaymentMethodCommand>(dto);
            var id = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentMethodDto dto, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new UpdatePaymentMethodCommand(id, dto.Name, dto.Type), cancellationToken);

            if (!success)
                return Ok(new ApiResponse { status = -1, message = "Update Failed" });

            return Ok(new ApiResponse { message = "Updated Successfully" });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new DeletePaymentMethodCommand(id), cancellationToken);

            if (!success)
                return NotFound();

            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
