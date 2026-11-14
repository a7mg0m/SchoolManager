using AutoMapper;
using BuildingBlocks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Identity.Application;

namespace SchoolManager.Identity.API.Controllers
{
    [ApiController]
    [Route("api/user-profile")]
    public class UserProfilesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateMyProfileCommand> _updateMyProfileValidator;

        public UserProfilesController(IMediator mediator, IMapper mapper, IValidator<UpdateMyProfileCommand> updateMyProfileValidator)
        {
            _mediator = mediator;
            _mapper = mapper;
            _updateMyProfileValidator = updateMyProfileValidator;
        }

        /// <summary>Profile of the logged-in user (user id is taken from the JWT).</summary>
        [Authorize]
        [HttpGet("my-profile")]
        public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new GetMyProfileQuery(), cancellationToken);
                return Ok(new ApiResponse { data = result });
            }
            catch (DomainException ex)
            {
                return MyProfileError(ex);
            }
        }

        /// <summary>Update the logged-in user's profile; returns the updated profile.</summary>
        [Authorize]
        [HttpPut("update-my-profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileDto dto, CancellationToken cancellationToken)
        {
            var command = new UpdateMyProfileCommand(
                dto.FirstName, dto.LastName, dto.Email, dto.PhoneNumber, dto.Address,
                dto.ProfileImage, dto.RemoveProfileImage);

            var validation = await _updateMyProfileValidator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
                return BadRequest(new ApiResponse(400, errors.First()) { errors = errors });
            }

            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                return Ok(new ApiResponse { message = "Updated Successfully", data = result });
            }
            catch (DomainException ex)
            {
                return MyProfileError(ex);
            }
        }

        private IActionResult MyProfileError(DomainException ex) => ex.Message switch
        {
            ErrorKeys.UserNotAuthenticated => Unauthorized(new ApiResponse(401, ex.Message)),
            ErrorKeys.UserNotFound => NotFound(new ApiResponse(404, ex.Message)),
            _ => BadRequest(new ApiResponse(400, ex.Message))
        };

        [HttpGet("get-all")]
        public async Task<ActionResult<List<UserProfileDto>>> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllUserProfilesQuery(), cancellationToken);
            var response = new ApiResponse { data = result };
            return Ok(response);
        }

        [HttpGet("get/{id}")]
        public async Task<ActionResult<UserProfileDto>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetUserProfileByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            var response = new ApiResponse { data = result };
            return Ok(response);
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<UserProfileDto>>> Search([FromQuery] string searchTerm, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new SearchUserProfilesQuery(searchTerm), cancellationToken);
            var response = new ApiResponse { data = result };
            return Ok(response);
        }

        [HttpPost("create")]
        public async Task<ActionResult<Guid>> Create([FromBody] CreateUserProfileDto dto, CancellationToken cancellationToken)
        {
            var command = _mapper.Map<CreateUserProfileCommand>(dto);
            var id = await _mediator.Send(command, cancellationToken);
            var response = new ApiResponse { InsertedId = id.ToString() };
            return CreatedAtAction(nameof(GetById), new { id }, response);
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateUserProfileDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var command = _mapper.Map<UpdateUserProfileCommand>(dto);
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
            var success = await _mediator.Send(new DeleteUserProfileCommand(id), cancellationToken);

            if (!success)
                return NotFound();

            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
