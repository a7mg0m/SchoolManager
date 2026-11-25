namespace SchoolManager.Notification.Application
{
    /// <summary>Where an uploaded picture is kept (a folder of wwwroot)</summary>
    public enum ImageFolder
    {
        /// <summary>wwwroot/notifications</summary>
        Notifications,

        /// <summary>wwwroot/events</summary>
        Events
    }

    /// <summary>
    /// Pictures of notifications and events, saved under the service's wwwroot and served at
    /// "/notification-files/…" (through the gateway). Stored in the database as that relative URL.
    /// </summary>
    public interface IImageFileStore
    {
        /// <summary>Saves the picture under a new unique name; returns its URL (e.g. /notification-files/notifications/{id}.jpg)</summary>
        Task<string> SaveAsync(Stream content, string extension, ImageFolder folder, CancellationToken cancellationToken = default);

        /// <summary>Deletes an uploaded picture (default pictures and outside URLs are never deleted); never throws</summary>
        void Delete(string? url);
    }

    /// <summary>Pictures shown when a notification / an event has none (wwwroot/default)</summary>
    public static class DefaultImages
    {
        public const string Notification = "/notification-files/default/notification.png";
        public const string Event = "/notification-files/default/event.png";

        /// <summary>Notification of a homework published by a teacher</summary>
        public const string Homework = "/notification-files/default/homework-default.png";

        /// <summary>Notification of a new exam</summary>
        public const string Exam = "/notification-files/default/exam-default.png";

        /// <summary>Notification of a bus trip that started</summary>
        public const string BusMoving = "/notification-files/default/bus-moving.png";
    }
}
