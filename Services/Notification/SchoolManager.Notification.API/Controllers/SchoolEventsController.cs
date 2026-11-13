using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.API.Controllers
{
    [Route("api/school-event")]
    public class SchoolEventsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public SchoolEventsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllSchoolEventsQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSchoolEventByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateSchoolEventDto dto, CancellationToken cancellationToken)
        {
            var command = new CreateSchoolEventCommand(dto.Title, dto.Description, dto.EventDate, dto.StartTime,
                dto.EndTime, dto.ImageUrl, dto.EventFor, dto.AcademicYearId, dto.SemesterId, dto.Targets);
            var id = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse { InsertedId = id.ToString() });
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateSchoolEventCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse { message = "Updated Successfully" });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteSchoolEventCommand(id), cancellationToken);
            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
