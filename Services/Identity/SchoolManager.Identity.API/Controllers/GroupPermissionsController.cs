using BuildingBlocks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Identity.Application;

namespace SchoolManager.Identity.API.Controllers
{
    [ApiController]
    [Route("api/group-permission")]
    public class GroupPermissionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IValidator<SaveGroupPermissionsCommand> _saveValidator;

        public GroupPermissionsController(IMediator mediator, IValidator<SaveGroupPermissionsCommand> saveValidator)
        {
            _mediator = mediator;
            _saveValidator = saveValidator;
        }

        /// <summary>Whole menu tree with the permissions of one employee group (for the permission screen).</summary>
        [HttpGet("get-by-group/{groupId}")]
        public async Task<IActionResult> GetByGroup(Guid groupId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetGroupPermissionsQuery(groupId), cancellationToken);
            if (result is null)
                return NotFound(new ApiResponse(404, ErrorKeys.EmployeeGroupNotFound));

            return Ok(new ApiResponse { data = result });
        }

        /// <summary>Replace all permissions of a group (items left out or with all flags false lose access).</summary>
        [HttpPut("save")]
        public async Task<IActionResult> Save([FromBody] SaveGroupPermissionsDto dto, CancellationToken cancellationToken)
        {
            var command = new SaveGroupPermissionsCommand(dto.EmployeeGroupId, dto.Permissions ?? new List<GroupPermissionDto>());

            var validation = await _saveValidator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
                return BadRequest(new ApiResponse(400, errors.First()) { errors = errors });
            }

            var summary = await _mediator.Send(command, cancellationToken);
            if (summary is null)
                return NotFound(new ApiResponse(404, ErrorKeys.EmployeeGroupNotFound));

            return Ok(new ApiResponse { message = "Saved Successfully", data = summary });
        }

        /// <summary>Menu of the logged-in user with their permissions (user id from the JWT).</summary>
        [Authorize]
        [HttpGet("my-menu")]
        public async Task<IActionResult> GetMyMenu(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new GetMyMenuQuery(), cancellationToken);
                return Ok(new ApiResponse { data = result });
            }
            catch (DomainException ex) when (ex.Message == ErrorKeys.UserNotAuthenticated)
            {
                return Unauthorized(new ApiResponse(401, ex.Message));
            }
        }
    }
}
