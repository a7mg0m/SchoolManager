using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
    public class SchoolAlbumItemConfiguration : IEntityTypeConfiguration<SchoolAlbumItem>
    {
        public void Configure(EntityTypeBuilder<SchoolAlbumItem> builder)
        {
            builder.ToTable("SchoolAlbumItems");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Url).IsRequired().HasMaxLength(500);
            builder.Property(x => x.FileName).IsRequired().HasMaxLength(260);
            builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100);

            // Deleting the school info deletes its album rows
            builder.HasOne<SchoolInfo>()
                .WithMany(s => s.Album)
                .HasForeignKey(x => x.SchoolInfoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
