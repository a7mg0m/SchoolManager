using AutoMapper;
using BuildingBlocks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Management.Application;

namespace SchoolManager.Management.API.Controllers
{
	[ApiController]
	[Route("api/teacher")]
	public class TeachersController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly IMapper _mapper;
		private readonly IValidator<CreateTeacherCommand> _createValidator;
		private readonly IValidator<UpdateTeacherCommand> _updateValidator;

		public TeachersController(
			IMediator mediator,
			IMapper mapper,
			IValidator<CreateTeacherCommand> createValidator,
			IValidator<UpdateTeacherCommand> updateValidator)
		{
			_mediator = mediator;
			_mapper = mapper;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}

		[HttpGet("get-all")]
		public async Task<ActionResult<List<TeacherDto>>> GetAll(CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetAllTeachersQuery(), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		[HttpGet("get/{id}")]
		public async Task<ActionResult<TeacherDto>> GetById(Guid id, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetTeacherByIdQuery(id), cancellationToken);
			if (result is null)
				return NotFound();

			return Ok(new ApiResponse { data = result });
		}

		/// <summary>Creates the teacher; the Identity service creates its login User and UserProfile (via Kafka).</summary>
		[HttpPost("create")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateTeacherDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<CreateTeacherCommand>(dto);

			var validation = await _createValidator.ValidateAsync(command, cancellationToken);
			if (!validation.IsValid)
				return ValidationFailed(validation);

			var id = await _mediator.Send(command, cancellationToken);
			return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
		}

		/// <summary>Updates the teacher; the Identity service updates its login User and UserProfile (via Kafka).</summary>
		[HttpPut("update")]
		public async Task<IActionResult> Update([FromBody] UpdateTeacherDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<UpdateTeacherCommand>(dto);

			var validation = await _updateValidator.ValidateAsync(command, cancellationToken);
			if (!validation.IsValid)
				return ValidationFailed(validation);

			var success = await _mediator.Send(command, cancellationToken);

			if (!success)
				return Ok(new ApiResponse { status = -1, message = "Update Failed" });

			return Ok(new ApiResponse { message = "Updated Successfully" });
		}

		/// <summary>Deletes the teacher; the Identity service deletes its login User and UserProfile (via Kafka).</summary>
		[HttpDelete("delete/{id}")]
		public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
		{
			var success = await _mediator.Send(new DeleteTeacherCommand(id), cancellationToken);

			if (!success)
				return NotFound();

			return Ok(new ApiResponse { message = "Deleted Successfully" });
		}

		/// <summary>
		/// Re-sends all teachers to the Identity service, creating missing logins
		/// (e.g. for teachers added before the Kafka sync existed). Safe to call more than once.
		/// </summary>
		[HttpPost("sync-accounts")]
		public async Task<IActionResult> SyncAccounts(CancellationToken cancellationToken)
		{
			var count = await _mediator.Send(new SyncTeacherAccountsCommand(), cancellationToken);
			return Ok(new ApiResponse { message = "Sync Queued", data = count });
		}

		private BadRequestObjectResult ValidationFailed(FluentValidation.Results.ValidationResult validation)
		{
			var errors = validation.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
			return BadRequest(new ApiResponse(400, errors.First()) { errors = errors });
		}
	}
}
