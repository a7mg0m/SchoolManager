using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.Notification.Application;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.API.Controllers
{
    /// <summary>
    /// Mobile apps' Firebase tokens for push notifications. The user (and parent / teacher / driver) comes from the
    /// login token.
    /// </summary>
    [Authorize]
    [Route("api/devices")]
    public class DevicesController(IMediator mediator) : ApiControllerBase
    {
        /// <summary>Registers (or moves to the logged-in user) the app's token; called after login and on app start</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDeviceRequest request, CancellationToken cancellationToken)
        {
            await mediator.Send(new RegisterDeviceTokenCommand(request.Token ?? string.Empty, request.Platform, request.AppName ?? string.Empty),
                cancellationToken);
            return Ok(new ApiResponse { message = "Registered Successfully" });
        }

        /// <summary>Removes the token at logout (only the logged-in user's own token; the token is URL-encoded)</summary>
        [HttpDelete("{token}")]
        public async Task<IActionResult> Unregister(string token, CancellationToken cancellationToken)
        {
            var removed = await mediator.Send(new UnregisterDeviceTokenCommand(token), cancellationToken);
            return removed
                ? Ok(new ApiResponse { message = "Unregistered Successfully" })
                : NotFound(new ApiResponse(404, "DeviceNotFound"));
        }
    }

    /// <summary>Body of POST api/devices/register (platform: 1 Android, 2 iOS, 3 Web; appName: parent | teacher | driver)</summary>
    public record RegisterDeviceRequest(string? Token, DevicePlatform Platform, string? AppName);
}
