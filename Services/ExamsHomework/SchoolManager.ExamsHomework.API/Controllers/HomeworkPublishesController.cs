using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.ExamsHomework.Application;

namespace SchoolManager.ExamsHomework.API.Controllers
{
    [Route("api/homework-publish")]
    public class HomeworkPublishesController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public HomeworkPublishesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllHomeworkPublishesQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetHomeworkPublishByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get-by-homework/{homeworkId}")]
        public async Task<IActionResult> GetByHomeworkId(Guid homeworkId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetHomeworkPublishesByHomeworkIdQuery(homeworkId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateHomeworkPublishDto dto, CancellationToken cancellationToken)
        {
            var command = new CreateHomeworkPublishCommand(dto.HomeworkId, dto.GradeId, dto.SectionId, dto.Status, CurrentUserId);
            var id = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse { InsertedId = id.ToString() });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteHomeworkPublishCommand(id), cancellationToken);
            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
