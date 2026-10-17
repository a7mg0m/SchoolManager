using AutoMapper;
using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Management.Application;

namespace SchoolManager.Management.API.Controllers
{
	[ApiController]
	[Route("api/semester")]
	public class SemestersController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly IMapper _mapper;

		public SemestersController(IMediator mediator, IMapper mapper)
		{
			_mediator = mediator;
			_mapper = mapper;
		}

		[HttpGet("get-all")]
		public async Task<ActionResult<List<SemesterDto>>> GetAll(CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetAllSemestersQuery(), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		[HttpGet("get/{id}")]
		public async Task<ActionResult<SemesterDto>> GetById(Guid id, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetSemesterByIdQuery(id), cancellationToken);
			if (result is null)
				return NotFound();

			return Ok(new ApiResponse { data = result });
		}

		[HttpPost("create")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateSemesterDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<CreateSemesterCommand>(dto);
			var id = await _mediator.Send(command, cancellationToken);
			return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
		}

		[HttpPut("update")]
		public async Task<IActionResult> Update([FromBody] UpdateSemesterDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<UpdateSemesterCommand>(dto);
			var success = await _mediator.Send(command, cancellationToken);

			if (!success)
				return Ok(new ApiResponse { status = -1, message = "Update Failed" });

			return Ok(new ApiResponse { message = "Updated Successfully" });
		}
		[HttpDelete("delete/{id}")]
		public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
		{
			var success = await _mediator.Send(new DeleteSemesterCommand(id), cancellationToken);

			if (!success)
				return NotFound();

			return Ok(new ApiResponse { message = "Deleted Successfully" });
		}
	}
}
