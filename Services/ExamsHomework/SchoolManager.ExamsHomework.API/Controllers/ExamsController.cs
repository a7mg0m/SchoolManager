using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.ExamsHomework.Application;

namespace SchoolManager.ExamsHomework.API.Controllers
{
    [Route("api/exam")]
    public class ExamsController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IManagementClient _management;

        public ExamsController(IMediator mediator, IManagementClient management)
        {
            _mediator = mediator;
            _management = management;
        }

        /// <summary>
        /// Exams of one of the logged-in parent's children with the child's own results (parent app).
        /// The child must be one of the parent's children (checked with the Management service); otherwise 404.
        /// </summary>
        [Authorize]
        [HttpGet("my-child/{studentId}")]
        public async Task<IActionResult> GetMyChildExams(Guid studentId, CancellationToken cancellationToken)
        {
            if (CurrentParentId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAParent"));

            var authorization = Request.Headers.Authorization.ToString();
            var children = await _management.GetMyChildrenAsync(authorization, cancellationToken);
            if (children is null)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable"));

            var child = children.FirstOrDefault(c => c.Id == studentId);
            if (child is null)
                return NotFound(new ApiResponse(404, "StudentNotFound"));

            var subjectNames = await _management.GetSubjectNamesAsync(authorization, cancellationToken);
            var exams = await _mediator.Send(new GetChildExamsQuery(child, subjectNames), cancellationToken);
            return Ok(new ApiResponse { data = exams });
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
            var markError = ValidateMarks(dto.Weight, dto.SuccessScore) ?? ValidateMonth(dto.ExamType, dto.Month);
            if (markError is not null)
                return BadRequest(new ApiResponse(400, markError));

            var command = new CreateExamCommand(dto.SubjectId, dto.StageId, dto.GradeId, dto.SectionId, dto.AcademicYearId,
                dto.SemesterId, dto.ExamType, dto.ExamDate, dto.StartTime, dto.EndTime, dto.Duration, dto.Weight, dto.SuccessScore, dto.AnswerModelUrl,
                dto.Month);
            var id = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse { InsertedId = id.ToString() });
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateExamCommand command, CancellationToken cancellationToken)
        {
            var markError = ValidateMarks(command.Weight, command.SuccessScore) ?? ValidateMonth(command.ExamType, command.Month);
            if (markError is not null)
                return BadRequest(new ApiResponse(400, markError));

            // The full mark can't drop below scores already given
            var results = await _mediator.Send(new GetExamResultsByExamIdQuery(command.Id), cancellationToken);
            if (results.Any(r => r.Score > command.Weight))
                return BadRequest(new ApiResponse(400, "WeightBelowExistingScores"));

            await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse { message = "Updated Successfully" });
        }

        /// <summary>Weight (full mark) must be > 0 and SuccessScore within 0..Weight</summary>
        private static string? ValidateMarks(double weight, double successScore)
        {
            if (!double.IsFinite(weight) || weight <= 0)
                return "WeightInvalid";
            if (!double.IsFinite(successScore) || successScore < 0 || successScore > weight)
                return "SuccessScoreInvalid";
            return null;
        }

        /// <summary>A Monthly exam needs its month (1..12); for the other types the month is ignored</summary>
        private static string? ValidateMonth(Domain.ExamType examType, int? month)
        {
            if (examType != Domain.ExamType.Monthly)
                return null;
            if (month is null)
                return "MonthRequired";
            if (month is < 1 or > 12)
                return "MonthInvalid";
            return null;
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteExamCommand(id), cancellationToken);
            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
