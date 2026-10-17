using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.ExamsHomework.Application;

namespace SchoolManager.ExamsHomework.API.Controllers
{
    [Route("api/homework-question")]
    public class HomeworkQuestionsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public HomeworkQuestionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllHomeworkQuestionsQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetHomeworkQuestionByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get-by-homework/{homeworkId}")]
        public async Task<IActionResult> GetByHomeworkId(Guid homeworkId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetHomeworkQuestionsByHomeworkIdQuery(homeworkId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateHomeworkQuestionDto dto, CancellationToken cancellationToken)
        {
            var command = new CreateHomeworkQuestionCommand(dto.HomeworkId, dto.QuestionHeader, dto.Questions, dto.ImageUrl, CurrentUserId);
            var id = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse { InsertedId = id.ToString() });
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateHomeworkQuestionCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command with { UpdatedBy = CurrentUserId }, cancellationToken);
            return Ok(new ApiResponse { message = "Updated Successfully" });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteHomeworkQuestionCommand(id), cancellationToken);
            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
