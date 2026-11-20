using BuildingBlocks;
using SchoolManager.Attendance.Domain;

namespace SchoolManager.Attendance.Application
{
    /// <summary>The checks every parent query / command makes before using a student id</summary>
    public static class ParentChildrenCheck
    {
        /// <summary>
        /// The logged-in parent's children (throws when the user is not a parent), or null when the Management
        /// service could not answer.
        /// </summary>
        public static async Task<IReadOnlyCollection<Guid>?> MyChildrenAsync(
            ICurrentUserService currentUser, IParentChildrenService parentChildren, string authorization, CancellationToken cancellationToken)
        {
            if (currentUser.ParentId is null)
                throw new DomainException("Only a parent can do this.");

            return await parentChildren.GetMyChildrenIdsAsync(authorization, cancellationToken);
        }

        /// <summary>Throws when <paramref name="studentId"/> is not one of the parent's children</summary>
        public static void EnsureMyChild(IReadOnlyCollection<Guid> children, Guid studentId)
        {
            if (!children.Contains(studentId))
                throw new DomainException("The student is not one of your children.");
        }
    }
}
