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

        /// <summary>Employee id of the logged-in user, or null when the user is not an employee.</summary>
        Guid? EmployeeId { get; }

        /// <summary>Teacher id of the logged-in user, or null when the user is not a teacher.</summary>
        Guid? TeacherId { get; }

        /// <summary>Parent id of the logged-in user, or null when the user is not a parent.</summary>
        Guid? ParentId { get; }

        /// <summary>Bus id of the logged-in bus driver, or null when the user is not a driver.</summary>
        Guid? BusId { get; }
    }
}
