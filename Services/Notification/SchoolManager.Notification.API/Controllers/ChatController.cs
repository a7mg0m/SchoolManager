using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Notification.Application;

namespace SchoolManager.Notification.API.Controllers
{
    [Route("api/chat")]
    public class ChatController(IMediator mediator) : ApiControllerBase
    {
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
