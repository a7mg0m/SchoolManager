using Microsoft.EntityFrameworkCore;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
        {
        }

        public DbSet<Domain.Notification> Notifications => Set<Domain.Notification>();
        public DbSet<NotificationTarget> NotificationTargets => Set<NotificationTarget>();
        public DbSet<SchoolEvent> SchoolEvents => Set<SchoolEvent>();
        public DbSet<EventTarget> EventTargets => Set<EventTarget>();

        public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
        public DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<MessageStatus> MessageStatuses => Set<MessageStatus>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
