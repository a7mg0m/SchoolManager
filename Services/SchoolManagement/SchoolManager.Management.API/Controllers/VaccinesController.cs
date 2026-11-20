using AutoMapper;
using BuildingBlocks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Management.Application;

namespace SchoolManager.Management.API.Controllers
{
	/// <summary>School clinic: vaccines</summary>
	[ApiController]
	[Route("api/vaccine")]
	public class VaccinesController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly IMapper _mapper;
		private readonly IValidator<CreateVaccineCommand> _createValidator;
		private readonly IValidator<UpdateVaccineCommand> _updateValidator;

		public VaccinesController(
			IMediator mediator,
			IMapper mapper,
			IValidator<CreateVaccineCommand> createValidator,
			IValidator<UpdateVaccineCommand> updateValidator)
		{
			_mediator = mediator;
			_mapper = mapper;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}

		[HttpGet("get-all")]
		public async Task<ActionResult<List<VaccineDto>>> GetAll(CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetAllVaccinesQuery(), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		[HttpGet("get-active")]
		public async Task<ActionResult<List<VaccineDto>>> GetActive(CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetActiveVaccinesQuery(), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		[HttpGet("get/{id}")]
		public async Task<ActionResult<VaccineDto>> GetById(Guid id, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetVaccineByIdQuery(id), cancellationToken);
			if (result is null)
				return NotFound();

			return Ok(new ApiResponse { data = result });
		}

		[HttpPut("activate/{id}")]
		public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
		{
			var success = await _mediator.Send(new SetVaccineActiveStatusCommand(id, true), cancellationToken);

			if (!success)
				return NotFound();

			return Ok(new ApiResponse { message = "Activated Successfully" });
		}

		[HttpPut("deactivate/{id}")]
		public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
		{
			var success = await _mediator.Send(new SetVaccineActiveStatusCommand(id, false), cancellationToken);

			if (!success)
				return NotFound();

			return Ok(new ApiResponse { message = "Deactivated Successfully" });
		}

		[HttpPost("create")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateVaccineDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<CreateVaccineCommand>(dto);

			var validation = await _createValidator.ValidateAsync(command, cancellationToken);
			if (!validation.IsValid)
				return ValidationFailed(validation);

			var id = await _mediator.Send(command, cancellationToken);
			return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
		}

		[HttpPut("update")]
		public async Task<IActionResult> Update([FromBody] UpdateVaccineDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<UpdateVaccineCommand>(dto);

			var validation = await _updateValidator.ValidateAsync(command, cancellationToken);
			if (!validation.IsValid)
				return ValidationFailed(validation);

			var success = await _mediator.Send(command, cancellationToken);

			if (!success)
				return Ok(new ApiResponse { status = -1, message = "Update Failed" });

			return Ok(new ApiResponse { message = "Updated Successfully" });
		}

		[HttpDelete("delete/{id}")]
		public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
		{
			var success = await _mediator.Send(new DeleteVaccineCommand(id), cancellationToken);

			if (!success)
				return NotFound();

			return Ok(new ApiResponse { message = "Deleted Successfully" });
		}

		private BadRequestObjectResult ValidationFailed(FluentValidation.Results.ValidationResult validation)
		{
			var errors = validation.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
			return BadRequest(new ApiResponse(400, errors.First()) { errors = errors });
		}
	}
}
