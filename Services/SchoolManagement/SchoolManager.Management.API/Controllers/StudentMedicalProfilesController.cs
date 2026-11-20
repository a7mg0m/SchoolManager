using AutoMapper;
using BuildingBlocks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Management.Application;

namespace SchoolManager.Management.API.Controllers
{
	/// <summary>School clinic: the medical profile of a student (one per student)</summary>
	[ApiController]
	[Route("api/student-medical-profile")]
	public class StudentMedicalProfilesController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly IMapper _mapper;
		private readonly IValidator<SaveStudentMedicalProfileCommand> _saveValidator;

		public StudentMedicalProfilesController(
			IMediator mediator,
			IMapper mapper,
			IValidator<SaveStudentMedicalProfileCommand> saveValidator)
		{
			_mediator = mediator;
			_mapper = mapper;
			_saveValidator = saveValidator;
		}

		[HttpGet("get-all")]
		public async Task<ActionResult<List<StudentMedicalProfileDto>>> GetAll(CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetAllStudentMedicalProfilesQuery(), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		/// <summary>The student's medical profile (null data when the student has none yet)</summary>
		[HttpGet("get/{studentId}")]
		public async Task<ActionResult<StudentMedicalProfileDto>> GetByStudentId(Guid studentId, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetStudentMedicalProfileByStudentIdQuery(studentId), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		/// <summary>Creates the student's medical profile, or updates it when the student has one</summary>
		[HttpPost("create")]
		public Task<IActionResult> Create([FromBody] SaveStudentMedicalProfileDto dto, CancellationToken cancellationToken)
			=> Save(dto, cancellationToken);

		/// <summary>Same as create (one profile per student)</summary>
		[HttpPut("update")]
		public Task<IActionResult> Update([FromBody] SaveStudentMedicalProfileDto dto, CancellationToken cancellationToken)
			=> Save(dto, cancellationToken);

		private async Task<IActionResult> Save(SaveStudentMedicalProfileDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<SaveStudentMedicalProfileCommand>(dto);

			var validation = await _saveValidator.ValidateAsync(command, cancellationToken);
			if (!validation.IsValid)
				return ValidationFailed(validation);

			var id = await _mediator.Send(command, cancellationToken);
			return Ok(new ApiResponse { InsertedId = id.ToString(), message = "Saved Successfully" });
		}

		[HttpDelete("delete/{id}")]
		public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
		{
			var success = await _mediator.Send(new DeleteStudentMedicalProfileCommand(id), cancellationToken);

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
