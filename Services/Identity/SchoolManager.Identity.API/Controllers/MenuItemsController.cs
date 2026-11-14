using AutoMapper;
using BuildingBlocks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Identity.Application;

namespace SchoolManager.Identity.API.Controllers
{
    [ApiController]
    [Route("api/menu-item")]
    public class MenuItemsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateMenuItemCommand> _createValidator;
        private readonly IValidator<UpdateMenuItemCommand> _updateValidator;

        public MenuItemsController(
            IMediator mediator,
            IMapper mapper,
            IValidator<CreateMenuItemCommand> createValidator,
            IValidator<UpdateMenuItemCommand> updateValidator)
        {
            _mediator = mediator;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        /// <summary>All menu items as a flat, ordered list (including inactive).</summary>
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllMenuItemsQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>Menu as a tree. activeOnly=true (default) is what the sidebar should use.</summary>
        [HttpGet("get-tree")]
        public async Task<IActionResult> GetTree([FromQuery] bool activeOnly = true, CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new GetMenuTreeQuery(activeOnly), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMenuItemByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound(new ApiResponse(404, ErrorKeys.MenuItemNotFound));

            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateMenuItemDto dto, CancellationToken cancellationToken)
        {
            var command = _mapper.Map<CreateMenuItemCommand>(dto);

            var validation = await _createValidator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
                return ValidationFailed(validation);

            try
            {
                var id = await _mediator.Send(command, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
            }
            catch (DomainException ex)
            {
                // MenuItemParentNotFound, MenuItemNameExists
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateMenuItemDto dto, CancellationToken cancellationToken)
        {
            var command = _mapper.Map<UpdateMenuItemCommand>(dto);

            var validation = await _updateValidator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
                return ValidationFailed(validation);

            try
            {
                var success = await _mediator.Send(command, cancellationToken);
                if (!success)
                    return NotFound(new ApiResponse(404, ErrorKeys.MenuItemNotFound));

                return Ok(new ApiResponse { message = "Updated Successfully" });
            }
            catch (DomainException ex)
            {
                // MenuItemParentNotFound, MenuItemParentInvalid, MenuItemNameExists
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        [HttpPut("activate/{id}")]
        public Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
            => SetActive(id, true, "Activated Successfully", cancellationToken);

        [HttpPut("deactivate/{id}")]
        public Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
            => SetActive(id, false, "Deactivated Successfully", cancellationToken);

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _mediator.Send(new DeleteMenuItemCommand(id), cancellationToken);
                if (!success)
                    return NotFound(new ApiResponse(404, ErrorKeys.MenuItemNotFound));

                return Ok(new ApiResponse { message = "Deleted Successfully" });
            }
            catch (DomainException ex)
            {
                // MenuItemHasChildren
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        private async Task<IActionResult> SetActive(Guid id, bool isActive, string message, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new SetMenuItemActiveStatusCommand(id, isActive), cancellationToken);
            if (!success)
                return NotFound(new ApiResponse(404, ErrorKeys.MenuItemNotFound));

            return Ok(new ApiResponse { message = message });
        }

        private BadRequestObjectResult ValidationFailed(FluentValidation.Results.ValidationResult validation)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
            return BadRequest(new ApiResponse(400, errors.First()) { errors = errors });
        }
    }
}
