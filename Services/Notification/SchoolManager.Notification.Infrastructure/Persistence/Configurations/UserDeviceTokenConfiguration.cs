using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Notification.Domain;

namespace SchoolManager.Notification.Infrastructure
{
    public class UserDeviceTokenConfiguration : IEntityTypeConfiguration<UserDeviceToken>
    {
        public void Configure(EntityTypeBuilder<UserDeviceToken> builder)
        {
            builder.ToTable("UserDeviceTokens");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Token).IsRequired().HasMaxLength(UserDeviceToken.TokenMaxLength);
            builder.Property(x => x.AppName).IsRequired().HasMaxLength(UserDeviceToken.AppNameMaxLength);
            builder.Property(x => x.UserType).IsRequired();
            builder.Property(x => x.Platform).IsRequired();
            builder.Property(x => x.LastSeenAt).IsRequired();

            // One row per device token; a user's devices are looked up for every push
            builder.HasIndex(x => x.Token).IsUnique();
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.UserType);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
