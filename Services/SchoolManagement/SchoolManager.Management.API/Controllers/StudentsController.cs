using AutoMapper;
using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Management.Application;

namespace SchoolManager.Management.API.Controllers
{
	[ApiController]
	[Route("api/student")]
	public class StudentsController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly IMapper _mapper;
		private readonly ICurrentUserService _currentUser;

		public StudentsController(IMediator mediator, IMapper mapper, ICurrentUserService currentUser)
		{
			_mediator = mediator;
			_mapper = mapper;
			_currentUser = currentUser;
		}

		[HttpGet("get-all")]
		public async Task<ActionResult<List<StudentDto>>> GetAll(CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetAllStudentsQuery(), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		/// <summary>
		/// Children of the logged-in parent (parent app). The parent id comes from the token's
		/// parent_id claim (UserProfile.ParentId in the Identity service), never from the request.
		/// </summary>
		[Authorize]
		[HttpGet("my-children")]
		public async Task<ActionResult<List<ChildDto>>> GetMyChildren(CancellationToken cancellationToken)
		{
			if (_currentUser.ParentId is not Guid parentId)
				return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAParent"));

			var result = await _mediator.Send(new GetMyChildrenQuery(parentId), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		/// <summary>
		/// Today's lessons of one of the logged-in parent's children (or the next school day's when
		/// none are left today), with the current / next lesson. 404 when the student is not the parent's child.
		/// </summary>
		[Authorize]
		[HttpGet("my-children/{studentId}/schedule")]
		public async Task<ActionResult<ChildScheduleDto>> GetMyChildSchedule(Guid studentId, CancellationToken cancellationToken)
		{
			if (_currentUser.ParentId is not Guid parentId)
				return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAParent"));

			var result = await _mediator.Send(new GetChildScheduleQuery(studentId, parentId, DateTime.Now), cancellationToken);
			if (result is null)
				return NotFound(new ApiResponse(404, "StudentNotFound"));

			return Ok(new ApiResponse { data = result });
		}

		/// <summary>
		/// The whole weekly timetable of one of the logged-in parent's children (parent app timetable page).
		/// 404 when the student is not the parent's child.
		/// </summary>
		[Authorize]
		[HttpGet("my-children/{studentId}/timetable")]
		public async Task<ActionResult<ChildTimetableDto>> GetMyChildTimetable(Guid studentId, CancellationToken cancellationToken)
		{
			if (_currentUser.ParentId is not Guid parentId)
				return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAParent"));

			var result = await _mediator.Send(new GetChildTimetableQuery(studentId, parentId, DateTime.Now), cancellationToken);
			if (result is null)
				return NotFound(new ApiResponse(404, "StudentNotFound"));

			return Ok(new ApiResponse { data = result });
		}

		[HttpGet("get/{id}")]
		public async Task<ActionResult<StudentDto>> GetById(Guid id, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetStudentByIdQuery(id), cancellationToken);
			if (result is null)
				return NotFound();

			return Ok(new ApiResponse { data = result });
		}

		[HttpPost("create")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateStudentDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<CreateStudentCommand>(dto);
			Guid id;
			try
			{
				id = await _mediator.Send(command, cancellationToken);
			}
			catch (DuplicateNationalIdException ex)
			{
				return Ok(new ApiResponse(-1, DuplicateNationalIdException.ErrorKey) { errors = ex.NationalIds });
			}
			return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
		}

		[HttpPut("update")]
		public async Task<IActionResult> Update([FromBody] UpdateStudentDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<UpdateStudentCommand>(dto);
			var success = await _mediator.Send(command, cancellationToken);

			if (!success)
				return Ok(new ApiResponse { status = -1, message = "Update Failed" });

			return Ok(new ApiResponse { message = "Updated Successfully" });
		}
		[HttpDelete("delete/{id}")]
		public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
		{
			var success = await _mediator.Send(new DeleteStudentCommand(id), cancellationToken);

			if (!success)
				return NotFound();

			return Ok(new ApiResponse { message = "Deleted Successfully" });
		}
	}
}
