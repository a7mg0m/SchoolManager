using AutoMapper;
using BuildingBlocks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Management.Application;

namespace SchoolManager.Management.API.Controllers
{
	/// <summary>School clinic: vaccination campaigns</summary>
	[ApiController]
	[Route("api/vaccination-campaign")]
	public class VaccinationCampaignsController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly IMapper _mapper;
		private readonly IValidator<CreateVaccinationCampaignCommand> _createValidator;
		private readonly IValidator<UpdateVaccinationCampaignCommand> _updateValidator;

		public VaccinationCampaignsController(
			IMediator mediator,
			IMapper mapper,
			IValidator<CreateVaccinationCampaignCommand> createValidator,
			IValidator<UpdateVaccinationCampaignCommand> updateValidator)
		{
			_mediator = mediator;
			_mapper = mapper;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}

		[HttpGet("get-all")]
		public async Task<ActionResult<List<VaccinationCampaignDto>>> GetAll(CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetAllVaccinationCampaignsQuery(), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		[HttpGet("get-active")]
		public async Task<ActionResult<List<VaccinationCampaignDto>>> GetActive(CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetActiveVaccinationCampaignsQuery(), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		[HttpGet("get/{id}")]
		public async Task<ActionResult<VaccinationCampaignDto>> GetById(Guid id, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetVaccinationCampaignByIdQuery(id), cancellationToken);
			if (result is null)
				return NotFound();

			return Ok(new ApiResponse { data = result });
		}

		[HttpPut("activate/{id}")]
		public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
		{
			var success = await _mediator.Send(new SetVaccinationCampaignActiveStatusCommand(id, true), cancellationToken);

			if (!success)
				return NotFound();

			return Ok(new ApiResponse { message = "Activated Successfully" });
		}

		[HttpPut("deactivate/{id}")]
		public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
		{
			var success = await _mediator.Send(new SetVaccinationCampaignActiveStatusCommand(id, false), cancellationToken);

			if (!success)
				return NotFound();

			return Ok(new ApiResponse { message = "Deactivated Successfully" });
		}

		[HttpPost("create")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateVaccinationCampaignDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<CreateVaccinationCampaignCommand>(dto);

			var validation = await _createValidator.ValidateAsync(command, cancellationToken);
			if (!validation.IsValid)
				return ValidationFailed(validation);

			var id = await _mediator.Send(command, cancellationToken);
			return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
		}

		[HttpPut("update")]
		public async Task<IActionResult> Update([FromBody] UpdateVaccinationCampaignDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<UpdateVaccinationCampaignCommand>(dto);

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
			var success = await _mediator.Send(new DeleteVaccinationCampaignCommand(id), cancellationToken);

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
