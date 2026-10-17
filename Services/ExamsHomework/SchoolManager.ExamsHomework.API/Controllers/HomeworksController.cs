using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.ExamsHomework.Application;

namespace SchoolManager.ExamsHomework.API.Controllers
{
    [Route("api/homework")]
    public class HomeworksController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public HomeworksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllHomeworksQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetHomeworkByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateHomeworkDto dto, CancellationToken cancellationToken)
        {
            var command = new CreateHomeworkCommand(dto.SubjectId, dto.TeacherId, dto.GradeId, dto.AcademicYearId,
                dto.SemesterId, dto.Title, dto.Description, dto.DueDate, dto.AttachmentUrl, CurrentUserId);
            var id = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse { InsertedId = id.ToString() });
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateHomeworkCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command with { UpdatedBy = CurrentUserId }, cancellationToken);
            return Ok(new ApiResponse { message = "Updated Successfully" });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteHomeworkCommand(id), cancellationToken);
            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
