using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Notification.Domain;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.API.Controllers
{
    [Authorize]
    [Route("api/chat")]
    public class ChatController(IMediator mediator, IManagementClient management) : ApiControllerBase
    {
        // ----- Teacher / parent apps: chats between teachers and the parents of their students -----

        /// <summary>
        /// Who the logged-in teacher (their students' parents) or parent (their children's teachers) may chat with.
        /// A parent passes the child chosen in the app (?studentId=, with ?academicYearId= / ?semesterId=): only the
        /// teachers of that child's section timetable.
        /// </summary>
        [HttpGet("my/contacts")]
        public async Task<IActionResult> GetMyContacts(
            [FromQuery] Guid? studentId, [FromQuery] Guid? academicYearId, [FromQuery] Guid? semesterId, CancellationToken cancellationToken)
        {
            var (contacts, error) = await ContactsAsync(cancellationToken, studentId, academicYearId, semesterId);
            return error ?? Ok(new ApiResponse { data = contacts!.OrderBy(c => c.NameAr).ToList() });
        }

        /// <summary>
        /// The logged-in user's private chats of the academic year chosen in the app (null: all), newest first,
        /// with the other person's names and unread counts.
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyChats([FromQuery] Guid? academicYearId, CancellationToken cancellationToken)
        {
            var (contacts, error) = await ContactsAsync(cancellationToken);
            if (error is not null) return error;

            var byId = contacts!.ToDictionary(c => c.UserId);
            // Names come from the contacts: Identity is not asked
            var chats = (await mediator.Send(new GetChatListQuery(CurrentUserId, academicYearId, ResolveNames: false), cancellationToken))
                .Where(c => c.Type == ChatRoomType.Private && c.OtherUserId is not null)
                .Select(c =>
                {
                    byId.TryGetValue(c.OtherUserId!.Value, out var contact);
                    return new MyChatDto(c.RoomId, c.OtherUserId.Value,
                        contact?.NameAr ?? c.Title, contact?.NameEn ?? c.Title,
                        contact?.DetailAr ?? string.Empty, contact?.DetailEn ?? string.Empty,
                        c.LastMessage, c.LastMessageSenderId, c.LastMessageOn, c.UnreadCount, c.LastAttachmentKind);
                })
                .ToList();
            return Ok(new ApiResponse { data = chats });
        }

        /// <summary>Opens (or creates) the private chat with one of the user's contacts in the academic year chosen in the app; returns its room id.</summary>
        [HttpPost("my")]
        public async Task<IActionResult> StartMyChat([FromBody] StartMyChatRequest request, CancellationToken cancellationToken)
        {
            var (contacts, error) = await ContactsAsync(cancellationToken);
            if (error is not null) return error;
            if (request is null || contacts!.All(c => c.UserId != request.OtherUserId))
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAContact"));
            if (request.AcademicYearId is not Guid yearId || yearId == Guid.Empty)
                return BadRequest(new ApiResponse(400, "AcademicYearRequired"));

            var id = await mediator.Send(new CreatePrivateChatCommand(CurrentUserId, request.OtherUserId, yearId), cancellationToken);
            return Ok(new ApiResponse { data = id });
        }

        /// <summary>Contacts by role (teacher / parent from the token), or the error to return</summary>
        private async Task<(IReadOnlyList<ChatContact>? Contacts, IActionResult? Error)> ContactsAsync(
            CancellationToken cancellationToken, Guid? studentId = null, Guid? academicYearId = null, Guid? semesterId = null)
        {
            bool asTeacher;
            if (CurrentTeacherId is not null) asTeacher = true;
            else if (CurrentParentId is not null) asTeacher = false;
            else return (null, StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotTeacherOrParent")));

            var contacts = await management.GetChatContactsAsync(
                Request.Headers.Authorization.ToString(), asTeacher, cancellationToken, studentId, academicYearId, semesterId);
            return contacts is null
                ? (null, StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable")))
                : (contacts, null);
        }

        // ----- Chat files: pictures, videos, voice notes (participants only) -----

        /// <summary>Request limit: the largest file (a video) plus the multipart envelope</summary>
        private const long MaxUploadRequestBytes = ChatMedia.MaxVideoBytes + 256 * 1024;

        /// <summary>
        /// Uploads a picture (≤ 10 MB), a short video (≤ 25 MB) or a voice note (≤ 10 MB) to the room. The type is
        /// checked by the file's first bytes, not only its name. Answers { data: AttachmentDto } to send with
        /// POST send-message (attachments).
        /// </summary>
        [HttpPost("{roomId:guid}/attachments")]
        [RequestSizeLimit(MaxUploadRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadRequestBytes)]
        public async Task<IActionResult> UploadAttachment(
            Guid roomId, IFormFile? file, [FromServices] IChatFileStore store, CancellationToken cancellationToken)
        {
            if (!await mediator.Send(new IsChatParticipantQuery(roomId, CurrentUserId), cancellationToken))
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotAParticipant"));
            if (file is null || file.Length == 0)
                return BadRequest(new ApiResponse(400, "FileRequired"));

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !ChatMedia.Types.TryGetValue(extension, out var type))
                return BadRequest(new ApiResponse(400, "FileTypeNotAllowed"));
            if (file.Length > ChatMedia.MaxBytes(type.Kind))
                return BadRequest(new ApiResponse(400, "FileTooLarge"));

            await using var stream = file.OpenReadStream();
            var header = new byte[16];
            var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
            if (!ChatMedia.LooksLike(type.Kind, extension, header.AsSpan(0, read)))
                return BadRequest(new ApiResponse(400, "FileTypeNotAllowed"));
            stream.Position = 0;

            var name = await store.SaveAsync(roomId, stream, extension, cancellationToken);
            var original = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(original) || original.Length > 100) original = $"file{extension.ToLowerInvariant()}";

            var attachment = new AttachmentDto(original, ChatMedia.Url(roomId, name), type.ContentType, file.Length);
            return Ok(new ApiResponse { message = "Uploaded Successfully", data = attachment });
        }

        /// <summary>
        /// A file of the room, for its participants only (others get 404). Range requests are supported so videos and
        /// voice notes can seek. The apps pass the JWT as ?access_token= (image / video / audio players can't set headers everywhere).
        /// </summary>
        [HttpGet("files/{roomId:guid}/{fileName}")]
        public async Task<IActionResult> GetFile(
            Guid roomId, string fileName, [FromServices] IChatFileStore store, CancellationToken cancellationToken)
        {
            if (!await mediator.Send(new IsChatParticipantQuery(roomId, CurrentUserId), cancellationToken))
                return NotFound();
            var stored = store.Find(roomId, fileName);
            if (stored is null) return NotFound();

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers.CacheControl = "private, max-age=604800";
            return PhysicalFile(stored.PhysicalPath, stored.ContentType, enableRangeProcessing: true);
        }

        // ----- Chat rooms (any participant) -----

        [HttpPost("send-message")]
        public async Task<ActionResult<SendMessageResponse>> SendMessage([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
        {
            var message = await mediator.Send(
                new SendMessageCommand(request.RoomId, CurrentUserId, request.Content ?? string.Empty, request.Attachments),
                cancellationToken);

            return Ok(new SendMessageResponse(
                message.Id, message.ChatRoomId, message.SenderId, message.SenderName,
                message.Content, message.SentOn, message.Status));
        }

        [HttpGet("list/{userId:guid}")]
        public async Task<ActionResult<IReadOnlyList<ChatListItemDto>>> GetChatList(Guid userId, CancellationToken cancellationToken) =>
            Ok(await mediator.Send(new GetChatListQuery(userId), cancellationToken));

        [HttpGet("history/{roomId:guid}")]
        public async Task<ActionResult<ChatHistoryDto>> GetHistory(
            Guid roomId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
            Ok(await mediator.Send(new GetChatHistoryQuery(roomId, CurrentUserId, page, pageSize), cancellationToken));

        [HttpPost("private")]
        public async Task<ActionResult<CreateChatResponse>> CreatePrivate([FromBody] CreatePrivateChatRequest request, CancellationToken cancellationToken)
        {
            var id = await mediator.Send(new CreatePrivateChatCommand(CurrentUserId, request.OtherUserId), cancellationToken);
            return Ok(new CreateChatResponse(id));
        }

        [HttpPost("group")]
        public async Task<ActionResult<CreateChatResponse>> CreateGroup([FromBody] CreateGroupChatRequest request, CancellationToken cancellationToken)
        {
            var id = await mediator.Send(
                new CreateGroupChatCommand(CurrentUserId, request.SectionId, request.Name, request.ParentIds ?? new List<Guid>()),
                cancellationToken);
            return Ok(new CreateChatResponse(id));
        }

        [HttpPut("history/{roomId:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid roomId, [FromBody] UpdateMessageStatusRequest request, CancellationToken cancellationToken)
        {
            await mediator.Send(
                new UpdateMessageStatusCommand(roomId, CurrentUserId, request.MessageIds ?? new List<Guid>(), request.Status),
                cancellationToken);
            return Ok(new ApiResponse { message = "Updated Successfully" });
        }
    }
}
