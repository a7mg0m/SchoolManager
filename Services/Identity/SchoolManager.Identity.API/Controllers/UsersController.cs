using AutoMapper;
using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Identity.Application;

namespace SchoolManager.Identity.API.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public UsersController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }
        [HttpGet("get-all")]
        public async Task<ActionResult<List<UserDto>>> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllUsersQuery(), cancellationToken);
            var response = new ApiResponse { data = result };
            return Ok(response);
        }

        [HttpGet("get/{id}")]
        public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            var response = new ApiResponse { data = result };
            return Ok(response);
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<UserDto>>> Search([FromQuery] string searchTerm, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new SearchUsersQuery(searchTerm), cancellationToken);
            var response = new ApiResponse { data = result };
            return Ok(response);
        }
        [HttpPost("create")]
        public async Task<ActionResult<Guid>> Create([FromBody] CreateUserDto dto, CancellationToken cancellationToken)
        {
            var command = _mapper.Map<CreateUserCommand>(dto);
            var id = await _mediator.Send(command, cancellationToken);
            var response = new ApiResponse { InsertedId = id.ToString() };
            return CreatedAtAction(nameof(GetById), new { id }, response);
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var command = _mapper.Map<UpdateUserCommand>(dto);
                var success = await _mediator.Send(command, cancellationToken);

                if (!success)
                    return Ok(new ApiResponse { status = -1, message = "Update Failed" });

                return Ok(new ApiResponse { message = "Updated Successfully" });
            }
            catch (DomainException ex)
            {
                return BadRequest(new ApiResponse { status = -1, message = "Update Failed", errors = ex.Message });
            }
           
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new DeleteUserCommand(id), cancellationToken);

            if (!success)
                return NotFound();

            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
        {
            var command = _mapper.Map<LoginCommand>(dto);

            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                return Ok(new ApiResponse { data = result });
            }
            catch (DomainException ex)
            {
                return Unauthorized(new ApiResponse { status = -1,message= "InvalidCredentials", errors = ex.Message });
            }
        }
    }
}
