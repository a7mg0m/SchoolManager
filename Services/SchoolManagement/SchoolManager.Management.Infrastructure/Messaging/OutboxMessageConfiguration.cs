using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SchoolManager.Management.Infrastructure
{
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.Topic).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Key).IsRequired().HasMaxLength(200);
            builder.Property(x => x.EventType).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Payload).IsRequired();
            builder.Property(x => x.LastError).HasMaxLength(2000);

            // The publisher reads pending messages in order
            builder.HasIndex(x => new { x.ProcessedOn, x.OccurredOn });
        }
    }
}
