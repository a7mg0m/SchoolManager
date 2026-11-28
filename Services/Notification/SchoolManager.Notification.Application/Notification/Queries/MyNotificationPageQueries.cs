using BuildingBlocks.CQRS;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Application
{
    /// <summary>A page of an app's notifications (newest first) with the totals by type (for the type chips)</summary>
    public class MyNotificationsPageDto
    {
        public List<MyNotificationDto> Items { get; set; } = new();
        /// <summary>How many there are in all (of the type asked)</summary>
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMore { get; set; }
        /// <summary>How many of each type (NotificationTypeEnum value → count), whatever type was asked</summary>
        public Dictionary<int, int> Counts { get; set; } = new();
    }

    /// <summary>Which page: 1-based page, 1..100 items, optionally one type</summary>
    public record NotificationPaging(int Page, int PageSize, NotificationTypeEnum? Type)
    {
        public int SafePage => Math.Max(1, Page);
        public int SafeSize => Math.Clamp(PageSize, 1, 100);
        public int Skip => (SafePage - 1) * SafeSize;
    }

    public record GetChildNotificationsPageQuery(ParentChild Child, Guid? AcademicYearId, NotificationPaging Paging) : IQuery<MyNotificationsPageDto>;
    public record GetTeacherNotificationsPageQuery(Guid TeacherId, Guid? AcademicYearId, NotificationPaging Paging) : IQuery<MyNotificationsPageDto>;
    public record GetDriverNotificationsPageQuery(Guid BusId, Guid? AcademicYearId, NotificationPaging Paging) : IQuery<MyNotificationsPageDto>;

    public class MyNotificationPageQueryHandlers(INotificationRepository repository) :
        IQueryHandler<GetChildNotificationsPageQuery, MyNotificationsPageDto>,
        IQueryHandler<GetTeacherNotificationsPageQuery, MyNotificationsPageDto>,
        IQueryHandler<GetDriverNotificationsPageQuery, MyNotificationsPageDto>
    {
        private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

        public async Task<MyNotificationsPageDto> Handle(GetChildNotificationsPageQuery q, CancellationToken cancellationToken)
            => ToDto(await repository.PageForStudentAsync(q.AcademicYearId, q.Child.GradeId, q.Child.SectionId ?? Guid.Empty, q.Child.Id,
                Today, q.Paging.Type, q.Paging.Skip, q.Paging.SafeSize, cancellationToken), q.Paging);

        public async Task<MyNotificationsPageDto> Handle(GetTeacherNotificationsPageQuery q, CancellationToken cancellationToken)
            => ToDto(await repository.PageForTeacherAsync(q.AcademicYearId, q.TeacherId,
                Today, q.Paging.Type, q.Paging.Skip, q.Paging.SafeSize, cancellationToken), q.Paging);

        public async Task<MyNotificationsPageDto> Handle(GetDriverNotificationsPageQuery q, CancellationToken cancellationToken)
            => ToDto(await repository.PageForDriverAsync(q.AcademicYearId, q.BusId,
                Today, q.Paging.Type, q.Paging.Skip, q.Paging.SafeSize, cancellationToken), q.Paging);

        private static MyNotificationsPageDto ToDto(NotificationPage page, NotificationPaging paging) => new()
        {
            Items = MyNotifications.ToDtos(page.Items),
            Total = page.Total,
            Page = paging.SafePage,
            PageSize = paging.SafeSize,
            HasMore = paging.Skip + page.Items.Count < page.Total,
            Counts = page.Counts.ToDictionary(c => (int)c.Key, c => c.Value)
        };
    }
}
