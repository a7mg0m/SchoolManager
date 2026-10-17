using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.ExamsHomework.Application;

namespace SchoolManager.ExamsHomework.API.Controllers
{
    [Route("api/exam")]
    public class ExamsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public ExamsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllExamsQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetExamByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateExamDto dto, CancellationToken cancellationToken)
        {
            var command = new CreateExamCommand(dto.SubjectId, dto.GradeId, dto.SectionId, dto.AcademicYearId,
                dto.SemesterId, dto.ExamType, dto.ExamDate, dto.StartTime, dto.EndTime, dto.Duration, dto.AnswerModelUrl, CurrentUserId);
            var id = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse { InsertedId = id.ToString() });
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateExamCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command with { UpdatedBy = CurrentUserId }, cancellationToken);
            return Ok(new ApiResponse { message = "Updated Successfully" });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteExamCommand(id), cancellationToken);
            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
