using AutoMapper;
using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Financial.Application;

namespace SchoolManager.Financial.API.Controllers
{
    [ApiController]
    [Route("api/expense")]
    public class ExpensesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public ExpensesController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllExpensesQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetExpenseByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateExpenseDto dto, CancellationToken cancellationToken)
        {
            var command = _mapper.Map<CreateExpenseCommand>(dto);
            var id = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseDto dto, CancellationToken cancellationToken)
        {
            var command = new UpdateExpenseCommand(id, dto.Title, dto.Description, dto.EventId, dto.CategoryId, dto.Type,
                dto.Amount, dto.Currency, dto.ExpenseDate, dto.Reference);
            var success = await _mediator.Send(command, cancellationToken);

            if (!success)
                return Ok(new ApiResponse { status = -1, message = "Update Failed" });

            return Ok(new ApiResponse { message = "Updated Successfully" });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new DeleteExpenseCommand(id), cancellationToken);

            if (!success)
                return NotFound();

            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
