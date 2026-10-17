using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
    public class SchoolInfoConfiguration : IEntityTypeConfiguration<SchoolInfo>
    {
        public void Configure(EntityTypeBuilder<SchoolInfo> builder)
        {
            builder.ToTable("SchoolInfos");
            builder.HasKey(x => x.Id);

            builder.ConfigureLocalized(x => x.Name, "Name");
            builder.Property(x => x.Address).HasMaxLength(500);
            builder.Property(x => x.Phone).HasMaxLength(50);
            builder.Property(x => x.Email).HasMaxLength(200);
            builder.Property(x => x.Logo).HasColumnType("varbinary(max)");
            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
