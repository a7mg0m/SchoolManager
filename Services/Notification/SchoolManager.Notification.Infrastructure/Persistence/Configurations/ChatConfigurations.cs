using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class ChatRoomConfiguration : IEntityTypeConfiguration<ChatRoom>
    {
        public void Configure(EntityTypeBuilder<ChatRoom> builder)
        {
            builder.ToTable("ChatRooms");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Ignore(x => x.DomainEvents);

            builder.Property(x => x.Type).HasConversion<int>().IsRequired();
            builder.Property(x => x.Name).HasMaxLength(200);
            builder.Property(x => x.PrivateKey).HasMaxLength(80);

            builder.HasIndex(x => x.PrivateKey).IsUnique().HasFilter("[PrivateKey] IS NOT NULL");
            builder.HasIndex(x => x.SectionId).IsUnique().HasFilter("[SectionId] IS NOT NULL");

            builder.HasMany(x => x.Participants)
                .WithOne()
                .HasForeignKey(p => p.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.Participants).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }

    public class ChatParticipantConfiguration : IEntityTypeConfiguration<ChatParticipant>
    {
        public void Configure(EntityTypeBuilder<ChatParticipant> builder)
        {
            builder.ToTable("ChatParticipants");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.UserId).IsRequired();
            builder.HasIndex(x => new { x.ChatRoomId, x.UserId }).IsUnique();
            builder.HasIndex(x => x.UserId);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }

    public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("ChatMessages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Ignore(x => x.DomainEvents);

            builder.Property(x => x.Content).IsRequired().HasMaxLength(ChatMessage.MaxContentLength);
            builder.HasIndex(x => new { x.ChatRoomId, x.SentOn });

            builder.HasOne<ChatRoom>()
                .WithMany()
                .HasForeignKey(x => x.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.OwnsMany(x => x.Attachments, a =>
            {
                a.ToTable("ChatMessageAttachments");
                a.WithOwner().HasForeignKey("ChatMessageId");
                a.Property<int>("Id");
                a.HasKey("Id");
                a.Property(p => p.FileName).IsRequired().HasMaxLength(255);
                a.Property(p => p.Url).IsRequired().HasMaxLength(1000);
                a.Property(p => p.ContentType).HasMaxLength(100);
                a.Property(p => p.SizeInBytes);
            });
            builder.Navigation(x => x.Attachments).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(x => x.Statuses)
                .WithOne()
                .HasForeignKey(s => s.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(x => x.Statuses).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }

    public class MessageStatusConfiguration : IEntityTypeConfiguration<MessageStatus>
    {
        public void Configure(EntityTypeBuilder<MessageStatus> builder)
        {
            builder.ToTable("MessageStatuses");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Ignore(x => x.DomainEvents);

            builder.Property(x => x.Status).HasConversion<int>().IsRequired();
            builder.HasIndex(x => new { x.MessageId, x.UserId }).IsUnique();
            builder.HasIndex(x => new { x.UserId, x.ChatRoomId, x.Status });

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
