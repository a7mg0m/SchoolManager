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
        private readonly FluentValidation.IValidator<ChangePasswordCommand> _changePasswordValidator;

        public UsersController(IMediator mediator, IMapper mapper, FluentValidation.IValidator<ChangePasswordCommand> changePasswordValidator)
        {
            _mediator = mediator;
            _mapper = mapper;
            _changePasswordValidator = changePasswordValidator;
        }

        /// <summary>Change the logged-in user's password (user id is taken from the JWT).</summary>
        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
        {
            var command = new ChangePasswordCommand(dto.CurrentPassword ?? string.Empty, dto.NewPassword ?? string.Empty);

            var validation = await _changePasswordValidator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
                return BadRequest(new ApiResponse(400, errors.First()) { errors = errors });
            }

            try
            {
                await _mediator.Send(command, cancellationToken);
                return Ok(new ApiResponse { message = "PasswordChanged" });
            }
            catch (DomainException ex) when (ex.Message == ErrorKeys.UserNotAuthenticated)
            {
                return Unauthorized(new ApiResponse(401, ex.Message));
            }
            catch (DomainException ex)
            {
                // CurrentPasswordInvalid is a 400 (not 401) so the app does not log the user out
                return BadRequest(new ApiResponse(400, ex.Message));
            }
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
