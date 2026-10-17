using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.ExamsHomework.Application;

namespace SchoolManager.ExamsHomework.API.Controllers
{
    [Route("api/exam-result")]
    public class ExamResultsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public ExamResultsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllExamResultsQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetExamResultByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get-by-exam/{examId}")]
        public async Task<IActionResult> GetByExamId(Guid examId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetExamResultsByExamIdQuery(examId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateExamResultDto dto, CancellationToken cancellationToken)
        {
            var command = new CreateExamResultCommand(dto.ExamId, dto.StudentId, dto.Score, CurrentUserId);
            var id = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse { InsertedId = id.ToString() });
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateExamResultCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command with { UpdatedBy = CurrentUserId }, cancellationToken);
            return Ok(new ApiResponse { message = "Updated Successfully" });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteExamResultCommand(id), cancellationToken);
            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
