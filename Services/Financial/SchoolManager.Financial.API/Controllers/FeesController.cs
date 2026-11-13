using AutoMapper;
using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Financial.Application;

namespace SchoolManager.Financial.API.Controllers
{
    [ApiController]
    [Route("api/fee")]
    public class FeesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public FeesController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllFeesQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetFeeByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateFeeDto dto, CancellationToken cancellationToken)
        {
            var command = _mapper.Map<CreateFeeCommand>(dto);
            var id = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFeeDto dto, CancellationToken cancellationToken)
        {
            var command = new UpdateFeeCommand(id, dto.Name, dto.Type, dto.Amount, dto.Currency, dto.IsMandatory);
            var success = await _mediator.Send(command, cancellationToken);

            if (!success)
                return Ok(new ApiResponse { status = -1, message = "Update Failed" });

            return Ok(new ApiResponse { message = "Updated Successfully" });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new DeleteFeeCommand(id), cancellationToken);

            if (!success)
                return NotFound();

            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
