using AutoMapper;
using BuildingBlocks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
		private readonly ICurrentUserService _currentUser;

		public TeachersController(
			IMediator mediator,
			IMapper mapper,
			IValidator<CreateTeacherCommand> createValidator,
			IValidator<UpdateTeacherCommand> updateValidator,
			ICurrentUserService currentUser)
		{
			_mediator = mediator;
			_mapper = mapper;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
			_currentUser = currentUser;
		}

		[HttpGet("get-all")]
		public async Task<ActionResult<List<TeacherDto>>> GetAll(CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetAllTeachersQuery(), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		/// <summary>
		/// The logged-in teacher's own record (read only, e.g. the job level shown in the teacher app).
		/// The teacher id comes from the login token.
		/// </summary>
		[Authorize]
		[HttpGet("my-profile")]
		public async Task<ActionResult<TeacherDto>> GetMyProfile(CancellationToken cancellationToken)
		{
			if (_currentUser.TeacherId is not Guid teacherId)
				return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

			var result = await _mediator.Send(new GetTeacherByIdQuery(teacherId), cancellationToken);
			if (result is null)
				return NotFound(new ApiResponse(404, "TeacherNotFound"));
			return Ok(new ApiResponse { data = result });
		}

		/// <summary>
		/// What the logged-in teacher teaches: subjects with their grade and classes, and the current academic
		/// year / semester (teacher app: homework). The teacher id comes from the login token.
		/// </summary>
		[Authorize]
		[HttpGet("my-classes")]
		public async Task<ActionResult<TeacherClassesDto>> GetMyClasses([FromQuery] Guid? academicYearId, [FromQuery] Guid? semesterId, CancellationToken cancellationToken)
		{
			if (_currentUser.TeacherId is not Guid teacherId)
				return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

			var result = await _mediator.Send(new GetMyClassesQuery(teacherId, academicYearId, semesterId), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		/// <summary>
		/// The logged-in teacher's lessons in all their classes for an academic year / semester
		/// (null: the current year / the semester running today). Teacher app timetable.
		/// </summary>
		[Authorize]
		[HttpGet("my-timetable")]
		public async Task<ActionResult<TeacherTimetableDto>> GetMyTimetable([FromQuery] Guid? academicYearId, [FromQuery] Guid? semesterId, CancellationToken cancellationToken)
		{
			if (_currentUser.TeacherId is not Guid teacherId)
				return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

			var result = await _mediator.Send(new GetMyTimetableQuery(teacherId, DateTime.Now, academicYearId, semesterId), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		/// <summary>
		/// Active students of one of the logged-in teacher's classes (teacher app: exam scores).
		/// 404 when the class is not in a grade the teacher teaches.
		/// </summary>
		[Authorize]
		[HttpGet("my-students/{sectionId}")]
		public async Task<ActionResult<List<TeacherStudentDto>>> GetMyStudents(Guid sectionId, CancellationToken cancellationToken)
		{
			if (_currentUser.TeacherId is not Guid teacherId)
				return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

			var result = await _mediator.Send(new GetMySectionStudentsQuery(teacherId, sectionId), cancellationToken);
			if (result is null)
				return NotFound(new ApiResponse(404, "SectionNotTaught"));
			return Ok(new ApiResponse { data = result });
		}

		/// <summary>
		/// Parents of the students in the logged-in teacher's classes, each with those children (teacher app: parents).
		/// </summary>
		[Authorize]
		[HttpGet("my-parents")]
		public async Task<ActionResult<List<TeacherParentDto>>> GetMyParents(CancellationToken cancellationToken)
		{
			if (_currentUser.TeacherId is not Guid teacherId)
				return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

			var result = await _mediator.Send(new GetMyParentsQuery(teacherId), cancellationToken);
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

			Guid id;
			try
			{
				id = await _mediator.Send(command, cancellationToken);
			}
			catch (TeacherAccountException ex)
			{
				// Login name / password problem (e.g. LoginNameExists, PasswordTooShort)
				return Ok(new ApiResponse(-1, ex.ErrorKey));
			}
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

			bool success;
			try
			{
				success = await _mediator.Send(command, cancellationToken);
			}
			catch (TeacherAccountException ex)
			{
				return Ok(new ApiResponse(-1, ex.ErrorKey));
			}

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
