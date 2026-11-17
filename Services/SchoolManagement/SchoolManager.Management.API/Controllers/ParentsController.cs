using AutoMapper;
using BuildingBlocks;
using MediatR;
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

		public ParentsController(IMediator mediator, IMapper mapper)
		{
			_mediator = mediator;
			_mapper = mapper;
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
