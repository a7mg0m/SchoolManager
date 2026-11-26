using AutoMapper;
using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Management.Application;

namespace SchoolManager.Management.API.Controllers
{
	[ApiController]
	[Route("api/parent")]
	public class ParentsController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly IMapper _mapper;
		private readonly ICurrentUserService _currentUser;

		public ParentsController(IMediator mediator, IMapper mapper, ICurrentUserService currentUser)
		{
			_mediator = mediator;
			_mapper = mapper;
			_currentUser = currentUser;
		}

		/// <summary>
		/// For the Identity service (its service token): does the parent still have a current child at the school?
		/// Only such parents may sign in to the parent app. App users (parents / teachers / drivers) get 403.
		/// </summary>
		[Authorize]
		[HttpGet("has-current-students/{parentId:guid}")]
		public async Task<IActionResult> HasCurrentStudents(Guid parentId, CancellationToken cancellationToken)
		{
			if (_currentUser.ParentId is not null || _currentUser.TeacherId is not null || _currentUser.BusId is not null)
				return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "Forbidden"));

			var result = await _mediator.Send(new HasCurrentStudentsQuery(parentId), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		[HttpGet("get-all")]
		public async Task<ActionResult<List<ParentDto>>> GetAll(CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetAllParentsQuery(), cancellationToken);
			return Ok(new ApiResponse { data = result });
		}

		[HttpGet("get/{id}")]
		public async Task<ActionResult<ParentDto>> GetById(Guid id, CancellationToken cancellationToken)
		{
			var result = await _mediator.Send(new GetParentByIdQuery(id), cancellationToken);
			if (result is null)
				return NotFound();

			return Ok(new ApiResponse { data = result });
		}

		[HttpPost("create")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateParentDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<CreateParentCommand>(dto);
			Guid id;
			try
			{
				id = await _mediator.Send(command, cancellationToken);
			}
			catch (DuplicateNationalIdException ex)
			{
				return Ok(new ApiResponse(-1, DuplicateNationalIdException.ErrorKey) { errors = ex.NationalIds });
			}
			catch (ParentAccountException ex)
			{
				// Login name / password problem (e.g. LoginNameExists, PasswordTooShort)
				return Ok(new ApiResponse(-1, ex.ErrorKey));
			}
			return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
		}

		[HttpPut("update")]
		public async Task<IActionResult> Update([FromBody] UpdateParentDto dto, CancellationToken cancellationToken)
		{
			var command = _mapper.Map<UpdateParentCommand>(dto);
			bool success;
			try
			{
				success = await _mediator.Send(command, cancellationToken);
			}
			catch (DuplicateNationalIdException ex)
			{
				return Ok(new ApiResponse(-1, DuplicateNationalIdException.ErrorKey) { errors = ex.NationalIds });
			}
			catch (ParentAccountException ex)
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
			var success = await _mediator.Send(new DeleteParentCommand(id), cancellationToken);

			if (!success)
				return NotFound();

			return Ok(new ApiResponse { message = "Deleted Successfully" });
		}

		/// <summary>
		/// Re-sends all parents to the Identity service, creating missing logins
		/// (e.g. for parents added before the Kafka sync existed). Safe to call more than once.
		/// </summary>
		[HttpPost("sync-accounts")]
		public async Task<IActionResult> SyncAccounts(CancellationToken cancellationToken)
		{
			var count = await _mediator.Send(new SyncParentAccountsCommand(), cancellationToken);
			return Ok(new ApiResponse { message = "Sync Queued", data = count });
		}
	}
}
