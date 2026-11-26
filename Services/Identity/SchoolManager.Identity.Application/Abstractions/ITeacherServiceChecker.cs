namespace SchoolManager.Identity.Application
{
    /// <summary>Asks the Management service whether a teacher is still in service (service not ended).</summary>
    public interface ITeacherServiceChecker
    {
        /// <summary>True / false; null when the Management service could not answer.</summary>
        Task<bool?> IsInServiceAsync(Guid teacherId, CancellationToken cancellationToken = default);
    }
}
