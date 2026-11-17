using AutoMapper;
using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Management.Application;

namespace SchoolManager.Management.API.Controllers
{
    [ApiController]
    [Route("api/school-info")]
    public class SchoolInfosController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public SchoolInfosController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [HttpGet("get-all")]
        public async Task<ActionResult<List<SchoolInfoDto>>> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllSchoolInfosQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result });
        }

        [HttpGet("get")]
        public async Task<ActionResult> Get(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllSchoolInfosQuery(), cancellationToken);
            return Ok(new ApiResponse { data = result.FirstOrDefault() });
        }

        [HttpGet("get/{id}")]
        public async Task<ActionResult<SchoolInfoDto>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSchoolInfoByIdQuery(id), cancellationToken);
            if (result is null)
                return NotFound();

            return Ok(new ApiResponse { data = result });
        }

        [HttpPost("create")]
        public async Task<ActionResult<Guid>> Create([FromBody] CreateSchoolInfoDto dto, CancellationToken cancellationToken)
        {
            var command = _mapper.Map<CreateSchoolInfoCommand>(dto);
            var id = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse { InsertedId = id.ToString() });
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateSchoolInfoDto dto, CancellationToken cancellationToken)
        {
            var command = _mapper.Map<UpdateSchoolInfoCommand>(dto);
            var success = await _mediator.Send(command, cancellationToken);

            if (!success)
                return Ok(new ApiResponse { status = -1, message = "Update Failed" });

            return Ok(new ApiResponse { message = "Updated Successfully" });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new DeleteSchoolInfoCommand(id), cancellationToken);

            if (!success)
                return NotFound();

            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }

        /// <summary>
        /// Uploads images to the school album (multipart/form-data, field "files"): each file is saved in
        /// wwwroot/upload/school-album and its URL added to the school info. Images only (jpg, png, gif, webp), 10 MB each.
        /// </summary>
        [Authorize]
        [HttpPost("album/upload")]
        [RequestSizeLimit(30 * 1024 * 1024)]
        public async Task<IActionResult> UploadAlbum([FromForm] List<IFormFile> files, CancellationToken cancellationToken)
        {
            var streams = new List<Stream>();
            try
            {
                var uploads = new List<AlbumFileUpload>();
                foreach (var file in files ?? new List<IFormFile>())
                {
                    var stream = file.OpenReadStream();
                    streams.Add(stream);
                    uploads.Add(new AlbumFileUpload(file.FileName, file.Length, stream));
                }

                var items = await _mediator.Send(new AddSchoolAlbumItemsCommand(uploads), cancellationToken);
                return Ok(new ApiResponse { message = "Uploaded Successfully", data = items });
            }
            catch (SchoolAlbumException ex)
            {
                return BadRequest(new ApiResponse(400, ex.ErrorKey) { errors = ex.FileName });
            }
            finally
            {
                foreach (var stream in streams)
                    await stream.DisposeAsync();
            }
        }

        /// <summary>Removes an image from the school album and deletes its file.</summary>
        [Authorize]
        [HttpDelete("album/delete/{id}")]
        public async Task<IActionResult> DeleteAlbumItem(Guid id, CancellationToken cancellationToken)
        {
            var success = await _mediator.Send(new DeleteSchoolAlbumItemCommand(id), cancellationToken);
            if (!success)
                return NotFound(new ApiResponse(404, "AlbumItemNotFound"));

            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }
    }
}
