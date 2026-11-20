using AutoMapper;
using BuildingBlocks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Management.Application;

namespace SchoolManager.Management.API.Controllers
{
	/// <summary>School clinic: visits (examination and diagnosis) with the medications given</summary>
	[ApiController]
	[Route("api/clinic-visit")]
	public class ClinicVisitsController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly IMapper _mapper;
		private readonly IValidator<CreateClinicVisitCommand> _createValidator;
		private readonly IValidator<UpdateClinicVisitCommand> _updateValidator;

		public ClinicVisitsController(
			IMediator mediator,
			IMapper mapper,
			IValidator<CreateClinicVisitCommand> createValidator,
			IValidator<UpdateClinicVisitCommand> updateValidator)
		{
			_mediator = mediator;
			_mapper = mapper;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}

		[HttpGet("get-all")]
		public async Task<ActionResult<List<ClinicVisitDto>>> GetAll(CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetAllClinicVisitsQuery(), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		[HttpGet("get/{id}")]
		public async Task<ActionResult<ClinicVisitDto>> GetById(Guid id, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetClinicVisitByIdQuery(id), cancellationToken);
			if (result is null)
				return NotFound();

			return Ok(new ApiResponse { data = result });
		}

		[HttpGet("get-by-student/{studentId}")]
		public async Task<ActionResult<List<ClinicVisitDto>>> GetByStudentId(Guid studentId, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetClinicVisitsByStudentIdQuery(studentId), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		[HttpPost("create")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateClinicVisitDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<CreateClinicVisitCommand>(dto);

			var validation = await _createValidator.ValidateAsync(command, cancellationToken);
			if (!validation.IsValid)
				return ValidationFailed(validation);

			var id = await _mediator.Send(command, cancellationToken);
			return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
		}

		[HttpPut("update")]
		public async Task<IActionResult> Update([FromBody] UpdateClinicVisitDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<UpdateClinicVisitCommand>(dto);

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
			var success = await _mediator.Send(new DeleteClinicVisitCommand(id), cancellationToken);

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
