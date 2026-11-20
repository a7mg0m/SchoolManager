using AutoMapper;
using BuildingBlocks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Management.Application;

namespace SchoolManager.Management.API.Controllers
{
	/// <summary>School clinic: the vaccine doses of students</summary>
	[ApiController]
	[Route("api/student-vaccination")]
	public class StudentVaccinationsController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly IMapper _mapper;
		private readonly IValidator<CreateStudentVaccinationCommand> _createValidator;
		private readonly IValidator<UpdateStudentVaccinationCommand> _updateValidator;

		public StudentVaccinationsController(
			IMediator mediator,
			IMapper mapper,
			IValidator<CreateStudentVaccinationCommand> createValidator,
			IValidator<UpdateStudentVaccinationCommand> updateValidator)
		{
			_mediator = mediator;
			_mapper = mapper;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}

		[HttpGet("get-all")]
		public async Task<ActionResult<List<StudentVaccinationDto>>> GetAll(CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetAllStudentVaccinationsQuery(), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		[HttpGet("get/{id}")]
		public async Task<ActionResult<StudentVaccinationDto>> GetById(Guid id, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetStudentVaccinationByIdQuery(id), cancellationToken);
			if (result is null)
				return NotFound();

			return Ok(new ApiResponse { data = result });
		}

		[HttpGet("get-by-student/{studentId}")]
		public async Task<ActionResult<List<StudentVaccinationDto>>> GetByStudentId(Guid studentId, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetStudentVaccinationsByStudentIdQuery(studentId), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		[HttpPost("create")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateStudentVaccinationDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<CreateStudentVaccinationCommand>(dto);

			var validation = await _createValidator.ValidateAsync(command, cancellationToken);
			if (!validation.IsValid)
				return ValidationFailed(validation);

			Guid id;
			try
			{
				id = await _mediator.Send(command, cancellationToken);
			}
			catch (StudentVaccinationException ex)
			{
				return Ok(new ApiResponse(-1, ex.ErrorKey));
			}
			return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
		}

		[HttpPut("update")]
		public async Task<IActionResult> Update([FromBody] UpdateStudentVaccinationDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<UpdateStudentVaccinationCommand>(dto);

			var validation = await _updateValidator.ValidateAsync(command, cancellationToken);
			if (!validation.IsValid)
				return ValidationFailed(validation);

			bool success;
			try
			{
				success = await _mediator.Send(command, cancellationToken);
			}
			catch (StudentVaccinationException ex)
			{
				return Ok(new ApiResponse(-1, ex.ErrorKey));
			}

			if (!success)
				return Ok(new ApiResponse { status = -1, message = "Update Failed" });

			return Ok(new ApiResponse { message = "Updated Successfully" });
		}

		[HttpDelete("delete/{id}")]
		public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
		{
			var success = await _mediator.Send(new DeleteStudentVaccinationCommand(id), cancellationToken);

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
