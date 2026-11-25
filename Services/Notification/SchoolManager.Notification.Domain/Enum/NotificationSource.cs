namespace SchoolManager.Notification.Domain
{
    /// <summary>Who created a notification</summary>
    public enum NotificationSource
    {
        /// <summary>The school office (admin notifications page)</summary>
        Admin = 0,

        /// <summary>The system, when a teacher published a homework (not listed in the admin page)</summary>
        Homework = 1,

        /// <summary>The system, when a new exam was saved (admin or teacher app; not listed in the admin page)</summary>
        Exam = 2,

        /// <summary>The system, when a bus trip started (driver app or admin; not listed in the admin page)</summary>
        Trip = 3
    }
}
