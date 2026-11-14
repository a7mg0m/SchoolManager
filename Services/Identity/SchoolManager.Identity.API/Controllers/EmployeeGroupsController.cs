using AutoMapper;
using BuildingBlocks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Identity.Application;

namespace SchoolManager.Identity.API.Controllers
{
    [ApiController]
    [Route("api/employee-group")]
    public class EmployeeGroupsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateEmployeeGroupCommand> _createValidator;
        private readonly IValidator<UpdateEmployeeGroupCommand> _updateValidator;

        public EmployeeGroupsController(
            IMediator mediator,
            IMapper mapper,
            IValidator<CreateEmployeeGroupCommand> createValidator,
            IValidator<UpdateEmployeeGroupCommand> updateValidator)
        {
            _mediator = mediator;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllEmployeeGroupsQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get-active")]
        public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetActiveEmployeeGroupsQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetEmployeeGroupByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound(new ApiResponse(404, ErrorKeys.EmployeeGroupNotFound));

            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateEmployeeGroupDto dto, CancellationToken cancellationToken)
        {
            var command = _mapper.Map<CreateEmployeeGroupCommand>(dto);

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
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateEmployeeGroupDto dto, CancellationToken cancellationToken)
        {
            var command = _mapper.Map<UpdateEmployeeGroupCommand>(dto);

            var validation = await _updateValidator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
                return ValidationFailed(validation);

            try
            {
                var success = await _mediator.Send(command, cancellationToken);
                if (!success)
                    return NotFound(new ApiResponse(404, ErrorKeys.EmployeeGroupNotFound));

                return Ok(new ApiResponse { message = "Updated Successfully" });
            }
            catch (DomainException ex)
            {
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
                var success = await _mediator.Send(new DeleteEmployeeGroupCommand(id), cancellationToken);
                if (!success)
                    return NotFound(new ApiResponse(404, ErrorKeys.EmployeeGroupNotFound));

                return Ok(new ApiResponse { message = "Deleted Successfully" });
            }
            catch (DomainException ex)
            {
                // e.g. EmployeeGroupHasEmployees
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        private async Task<IActionResult> SetActive(Guid id, bool isActive, string message, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new SetEmployeeGroupActiveStatusCommand(id, isActive), cancellationToken);
            if (!success)
                return NotFound(new ApiResponse(404, ErrorKeys.EmployeeGroupNotFound));

            return Ok(new ApiResponse { message = message });
        }

        private BadRequestObjectResult ValidationFailed(FluentValidation.Results.ValidationResult validation)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
            return BadRequest(new ApiResponse(400, errors.First()) { errors = errors });
        }
    }
}
