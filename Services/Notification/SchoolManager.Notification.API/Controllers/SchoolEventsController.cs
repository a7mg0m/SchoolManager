using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.API.Controllers
{
    [Route("api/school-event")]
    public class SchoolEventsController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IManagementClient _management;

        public SchoolEventsController(IMediator mediator, IManagementClient management)
        {
            _mediator = mediator;
            _management = management;
        }

        /// <summary>
        /// Uploads the picture of a school event (JPG / PNG / WEBP / GIF, up to 5 MB); answers its URL for ImageUrl.
        /// Saved under wwwroot/events.
        /// </summary>
        [Authorize]
        [HttpPost("upload-image")]
        [RequestSizeLimit(ImageUpload.MaxRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = ImageUpload.MaxRequestBytes)]
        public Task<IActionResult> UploadImage(IFormFile? file, [FromServices] IImageFileStore store, CancellationToken cancellationToken)
            => ImageUpload.SaveAsync(file, ImageFolder.Events, store, cancellationToken);

        /// <summary>
        /// Events that concern one of the logged-in parent's children (parent app): for everyone, its stage,
        /// grade or section, or the child itself. The child must be the parent's (checked with Management); otherwise 404.
        /// </summary>
        [Authorize]
        [HttpGet("my-child/{studentId}")]
        public async Task<IActionResult> GetMyChildEvents(Guid studentId, [FromQuery] Guid? academicYearId, [FromQuery] Guid? semesterId, CancellationToken cancellationToken)
        {
            if (CurrentParentId is null)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAParent"));

            var children = await _management.GetMyChildrenAsync(Request.Headers.Authorization.ToString(), cancellationToken);
            if (children is null)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable"));

            var child = children.FirstOrDefault(c => c.Id == studentId);
            if (child is null)
                return NotFound(new ApiResponse(404, "StudentNotFound"));

            // The academic year chosen in the app, else the child's own
            if (academicYearId is Guid year)
                child = child with { AcademicYearId = year };

            var events = await _mediator.Send(new GetChildEventsQuery(child, academicYearId, semesterId), cancellationToken);
            return Ok(new ApiResponse { data = events });
        }

        /// <summary>
        /// Events for the logged-in teacher (teacher app): for everyone, or for teachers when they are one of the
        /// chosen teachers (an event for teachers without targets is for all teachers).
        /// </summary>
        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyEvents([FromQuery] Guid? academicYearId, [FromQuery] Guid? semesterId, CancellationToken cancellationToken)
        {
            if (CurrentTeacherId is not Guid teacherId)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            var events = await _mediator.Send(new GetTeacherEventsQuery(teacherId, academicYearId, semesterId), cancellationToken);
            return Ok(new ApiResponse { data = events });
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
