using AutoMapper;
using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Management.Application;

namespace SchoolManager.Management.API.Controllers
{
	[ApiController]
	[Route("api/timetable")]
	public class TimetablesController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly IMapper _mapper;

		public TimetablesController(IMediator mediator, IMapper mapper)
		{
			_mediator = mediator;
			_mapper = mapper;
		}

		[HttpGet("get-all")]
		public async Task<ActionResult<List<TimetableDto>>> GetAll([FromQuery] Guid? academicYearId, [FromQuery] Guid? semesterId, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetAllTimetablesQuery(academicYearId, semesterId), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		[HttpGet("get/{id}")]
		public async Task<ActionResult<TimetableDto>> GetById(Guid id, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetTimetableByIdQuery(id), cancellationToken);
			if (result is null)
				return NotFound();

			return Ok(new ApiResponse { data = result });
		}

		[HttpPost("create")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateTimetableDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<CreateTimetableCommand>(dto);
			try
			{
				var id = await _mediator.Send(command, cancellationToken);
				return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
			}
			catch (TeacherClashException ex)
			{
				return BadRequest(new ApiResponse { status = 400, message = "TeacherClash", data = ex.Clashes });
			}
		}

		[HttpPut("update")]
		public async Task<IActionResult> Update([FromBody] UpdateTimetableDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<UpdateTimetableCommand>(dto);
			bool success;
			try
			{
				success = await _mediator.Send(command, cancellationToken);
			}
			catch (TeacherClashException ex)
			{
				return BadRequest(new ApiResponse { status = 400, message = "TeacherClash", data = ex.Clashes });
			}

			if (!success)
				return Ok(new ApiResponse { status = -1, message = "Update Failed" });

			return Ok(new ApiResponse { message = "Updated Successfully" });
		}
		[HttpDelete("delete/{id}")]
		public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
		{
			var success = await _mediator.Send(new DeleteTimetableCommand(id), cancellationToken);

			if (!success)
				return NotFound();

			return Ok(new ApiResponse { message = "Deleted Successfully" });
		}
	}
}
