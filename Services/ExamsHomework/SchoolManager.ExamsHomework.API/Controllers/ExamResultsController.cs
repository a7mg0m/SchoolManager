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
            var exam = await _mediator.Send(new GetExamByIdQuery(dto.ExamId), cancellationToken);
            if (exam is null)
                return NotFound(new ApiResponse(404, "ExamNotFound"));
            if (!IsValidScore(dto.Score, exam.Weight))
                return BadRequest(new ApiResponse(400, "ScoreInvalid"));

            var command = new CreateExamResultCommand(dto.ExamId, dto.StudentId, dto.Score, CurrentUserId);
            var id = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse { InsertedId = id.ToString() });
        }

        /// <summary>
        /// Save the results of one exam for many students at once.
        /// Score = null removes the student's existing result.
        /// </summary>
        [HttpPost("save-bulk")]
        public async Task<IActionResult> SaveBulk([FromBody] SaveExamResultsDto dto, CancellationToken cancellationToken)
        {
            if (dto is null || dto.ExamId == Guid.Empty)
                return BadRequest(new ApiResponse(400, "ExamRequired"));

            var results = dto.Results ?? new List<StudentScoreDto>();

            if (results.Any(r => r.StudentId == Guid.Empty))
                return BadRequest(new ApiResponse(400, "StudentRequired"));

            if (results.GroupBy(r => r.StudentId).Any(g => g.Count() > 1))
                return BadRequest(new ApiResponse(400, "DuplicateStudent"));

            var exam = await _mediator.Send(new GetExamByIdQuery(dto.ExamId), cancellationToken);
            if (exam is null)
                return NotFound(new ApiResponse(404, "ExamNotFound"));

            // Every score must be within 0..Weight (full mark)
            if (results.Any(r => r.Score is double s && !IsValidScore(s, exam.Weight)))
                return BadRequest(new ApiResponse(400, "ScoreInvalid"));

            var summary = await _mediator.Send(new SaveExamResultsCommand(dto.ExamId, results, CurrentUserId), cancellationToken);
            if (summary is null)
                return NotFound(new ApiResponse(404, "ExamNotFound"));

            return Ok(new ApiResponse { message = "Saved Successfully", data = summary });
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateExamResultCommand command, CancellationToken cancellationToken)
        {
            var existing = await _mediator.Send(new GetExamResultByIdQuery(command.Id), cancellationToken);
            if (existing is null)
                return NotFound(new ApiResponse(404, "ExamResultNotFound"));
            var exam = await _mediator.Send(new GetExamByIdQuery(existing.ExamId), cancellationToken);
            if (exam is null)
                return NotFound(new ApiResponse(404, "ExamNotFound"));
            if (!IsValidScore(command.Score, exam.Weight))
                return BadRequest(new ApiResponse(400, "ScoreInvalid"));

            await _mediator.Send(command with { UpdatedBy = CurrentUserId }, cancellationToken);
            return Ok(new ApiResponse { message = "Updated Successfully" });
        }

        /// <summary>A score must be a finite number within 0..weight (the exam's full mark)</summary>
        private static bool IsValidScore(double score, double weight)
            => double.IsFinite(score) && score >= 0 && score <= weight;

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteExamResultCommand(id), cancellationToken);
            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
