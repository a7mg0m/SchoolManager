namespace SchoolManager.Notification.Application
{
    /// <summary>
    /// Someone the logged-in user may chat with (from the Management service): for a teacher the parents of their
    /// students, for a parent the teachers of their children. The user id equals the teacher / parent id.
    /// </summary>
    /// <param name="DetailAr">Children ("ولي أمر سارة") or subjects ("رياضيات، علوم")</param>
    public sealed record ChatContact(Guid UserId, string NameAr, string NameEn, string DetailAr, string DetailEn);
}
