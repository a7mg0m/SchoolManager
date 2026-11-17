namespace SchoolManager.Notification.Application
{
    /// <summary>A child of the logged-in parent and where it studies (from the Management service).</summary>
    public sealed record ParentChild(Guid Id, Guid StageId, Guid GradeId, Guid SectionId, Guid AcademicYearId);
}
