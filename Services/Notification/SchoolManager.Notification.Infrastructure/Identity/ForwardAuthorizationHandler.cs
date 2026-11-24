using Microsoft.AspNetCore.Http;

namespace SchoolManager.Notification.Infrastructure
{
    /// <summary>Sends the current request's "Authorization" header with calls to the other services (through the gateway)</summary>
    public class ForwardAuthorizationHandler(IHttpContextAccessor accessor) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var authorization = accessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (request.Headers.Authorization is null && !string.IsNullOrWhiteSpace(authorization))
                request.Headers.TryAddWithoutValidation("Authorization", authorization);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
