using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.ExamsHomework.Application;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.API.Controllers
{
    [Route("api/homework")]
    public class HomeworksController : ApiControllerBase
    {
        private const int TitleMaxLength = 200;
        private const int DescriptionMaxLength = 2000;
        private const int QuestionHeaderMaxLength = 500;
        private const int MaxQuestions = 30;

        private readonly IMediator _mediator;
        private readonly IManagementClient _management;
        private readonly IHomeworkRepository _homeworks;

        public HomeworksController(IMediator mediator, IManagementClient management, IHomeworkRepository homeworks)
        {
            _mediator = mediator;
            _management = management;
            _homeworks = homeworks;
        }

        // ----- Teacher app: the logged-in teacher's homework -----

        /// <summary>The logged-in teacher's homework, newest due date first, with the classes each is published to.</summary>
        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyHomeworks([FromQuery] Guid? academicYearId, [FromQuery] Guid? semesterId, CancellationToken cancellationToken)
        {
            if (CurrentTeacherId is not Guid teacherId)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            var classes = await _management.GetMyClassesAsync(Request.Headers.Authorization.ToString(), academicYearId, semesterId, cancellationToken)
                ?? new TeacherClasses(null, null, Array.Empty<TeacherSubject>()); // names are optional here
            var result = await _mediator.Send(new GetTeacherHomeworksQuery(teacherId, classes, academicYearId, semesterId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        /// <summary>
        /// New homework of the logged-in teacher, for one of the subjects / grades they teach
        /// (checked with the Management service). The academic year and semester are the current ones.
        /// </summary>
        [Authorize]
        [HttpPost("my")]
        public async Task<IActionResult> CreateMyHomework([FromBody] CreateMyHomeworkDto dto, CancellationToken cancellationToken)
        {
            if (CurrentTeacherId is not Guid teacherId)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            var title = dto.Title?.Trim() ?? string.Empty;
            var description = dto.Description?.Trim() ?? string.Empty;
            var questions = (dto.Questions ?? new List<HomeworkQuestionItemDto>())
                .Where(q => !string.IsNullOrWhiteSpace(q.Header))
                .ToList();

            if (title.Length == 0) return BadRequest(new ApiResponse(400, "TitleRequired"));
            if (title.Length > TitleMaxLength) return BadRequest(new ApiResponse(400, "TitleTooLong"));
            if (description.Length > DescriptionMaxLength) return BadRequest(new ApiResponse(400, "DescriptionTooLong"));
            if (dto.DueDate < DateOnly.FromDateTime(DateTime.Today)) return BadRequest(new ApiResponse(400, "DueDateInPast"));
            if (questions.Count > MaxQuestions) return BadRequest(new ApiResponse(400, "TooManyQuestions"));
            if (questions.Any(q => q.Header.Trim().Length > QuestionHeaderMaxLength))
                return BadRequest(new ApiResponse(400, "QuestionTooLong"));

            var classes = await _management.GetMyClassesAsync(Request.Headers.Authorization.ToString(), dto.AcademicYearId, dto.SemesterId, cancellationToken);
            if (classes is null)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable"));
            if (classes.Subject(dto.SubjectId, dto.GradeId) is null)
                return BadRequest(new ApiResponse(400, "SubjectNotTaught"));
            if (classes.AcademicYearId is not Guid yearId || classes.SemesterId is not Guid semesterId)
                return BadRequest(new ApiResponse(400, "NoActiveSemester"));

            var id = await _mediator.Send(new CreateTeacherHomeworkCommand(
                teacherId, dto.SubjectId, dto.GradeId, yearId, semesterId,
                title, description, dto.DueDate, questions), cancellationToken);
            return Ok(new ApiResponse { InsertedId = id.ToString() });
        }

        /// <summary>
        /// Publish one of the logged-in teacher's homework to classes of its grade; parents of the students in
        /// those classes then see it. Classes already published to are skipped.
        /// </summary>
        [Authorize]
        [HttpPost("my/{id}/publish")]
        public async Task<IActionResult> PublishMyHomework(Guid id, [FromBody] PublishHomeworkDto dto, CancellationToken cancellationToken)
        {
            if (CurrentTeacherId is not Guid teacherId)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            var homework = await _homeworks.GetByIdAsync(id);
            if (homework is null || homework.TeacherId != teacherId)
                return NotFound(new ApiResponse(404, "HomeworkNotFound"));

            var sectionIds = (dto.SectionIds ?? new List<Guid>()).Distinct().ToList();
            if (sectionIds.Count == 0)
                return BadRequest(new ApiResponse(400, "NoSections"));

            var classes = await _management.GetMyClassesAsync(Request.Headers.Authorization.ToString(), cancellationToken: cancellationToken);
            if (classes is null)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable"));

            // Only classes of the homework's grade that the teacher teaches this subject in
            var allowed = classes.Subject(homework.SubjectId, homework.GradeId)?.Sections.Select(s => s.Id).ToHashSet()
                ?? new HashSet<Guid>();
            if (sectionIds.Any(s => !allowed.Contains(s)))
                return BadRequest(new ApiResponse(400, "SectionNotTaught"));

            var added = await _mediator.Send(new PublishHomeworkCommand(homework.Id, homework.GradeId, sectionIds), cancellationToken);
            return Ok(new ApiResponse { message = "Published", data = added });
        }

        /// <summary>Delete one of the logged-in teacher's homework (and its publishes).</summary>
        [Authorize]
        [HttpDelete("my/{id}")]
        public async Task<IActionResult> DeleteMyHomework(Guid id, CancellationToken cancellationToken)
        {
            if (CurrentTeacherId is not Guid teacherId)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            var homework = await _homeworks.GetByIdAsync(id);
            if (homework is null || homework.TeacherId != teacherId)
                return NotFound(new ApiResponse(404, "HomeworkNotFound"));

            await _mediator.Send(new DeleteHomeworkCommand(id), cancellationToken);
            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }

        // ----- Parent app: homework of the parent's child -----

        /// <summary>
        /// Homework published to the class of one of the logged-in parent's children (this academic year).
        /// The child must be one of the parent's children (checked with the Management service); otherwise 404.
        /// </summary>
        [Authorize]
        [HttpGet("my-child/{studentId}")]
        public async Task<IActionResult> GetMyChildHomeworks(Guid studentId, [FromQuery] Guid? academicYearId, [FromQuery] Guid? semesterId, CancellationToken cancellationToken)
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

            // The academic year chosen in the app, else the child's own
            if (academicYearId is Guid year)
                child = child with { AcademicYearId = year };

            var subjectNames = await _management.GetSubjectNamesAsync(authorization, cancellationToken);
            var result = await _mediator.Send(new GetChildHomeworksQuery(child, subjectNames, semesterId), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        // ----- Admin -----

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
                dto.SemesterId, dto.Title, dto.Description, dto.DueDate, dto.AttachmentUrl);
            var id = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse { InsertedId = id.ToString() });
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateHomeworkCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command, cancellationToken);
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
