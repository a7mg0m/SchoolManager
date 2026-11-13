namespace BuildingBlocks
{
    /// <summary>
    /// The user who sent the current request, read from the validated JWT.
    /// Inject it wherever the logged-in user is needed (e.g. CreatedBy / UpdatedBy in AuditInfo).
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>Id of the logged-in user, or <see cref="Guid.Empty"/> when the request is anonymous.</summary>
        Guid UserId { get; }

        /// <summary>Login name of the logged-in user, if available.</summary>
        string? UserName { get; }

        /// <summary>Email of the logged-in user, if available.</summary>
        string? Email { get; }

        /// <summary>True when the request carries a valid token with a user id.</summary>
        bool IsAuthenticated { get; }
    }
}
