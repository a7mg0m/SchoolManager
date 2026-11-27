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
        /// <summary>
        /// Whether a login name is free (optionally ignoring the user it already belongs to).
        /// Used by the admin parent page before saving, because parent logins are created asynchronously.
        /// </summary>
        [Authorize]
        [HttpGet("login-name-available")]
        public async Task<IActionResult> IsLoginNameAvailable([FromQuery] string loginName, [FromQuery] Guid? userId, CancellationToken cancellationToken)
        {
            var available = await _mediator.Send(new IsLoginNameAvailableQuery(loginName, userId), cancellationToken);
            return Ok(new ApiResponse { data = available });
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

        /// <summary>
        /// An app opened with a saved session: checks the user again (active, login name unchanged, teacher in service,
        /// parent with a current child) and answers a fresh token like the login does. 401 with the reason key in
        /// `errors` when the session must end; 503 when it could not be checked (the app keeps the session).
        /// </summary>
        [Authorize]
        [HttpPost("renew")]
        public async Task<ActionResult<LoginResultDto>> Renew([FromServices] ICurrentUserService currentUser, CancellationToken cancellationToken)
        {
            if (currentUser.UserId == Guid.Empty)
                return Unauthorized(new ApiResponse { status = -1, message = "InvalidCredentials", errors = ErrorKeys.UserInactive });

            try
            {
                var result = await _mediator.Send(new RenewSessionCommand(currentUser.UserId, currentUser.UserName), cancellationToken);
                return Ok(new ApiResponse { data = result });
            }
            catch (DomainException ex) when (ex.Message is ErrorKeys.TeacherCheckUnavailable or ErrorKeys.ParentCheckUnavailable)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse { status = -1, message = "Unavailable", errors = ex.Message });
            }
            catch (DomainException ex)
            {
                return Unauthorized(new ApiResponse { status = -1, message = "SessionEnded", errors = ex.Message });
            }
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
