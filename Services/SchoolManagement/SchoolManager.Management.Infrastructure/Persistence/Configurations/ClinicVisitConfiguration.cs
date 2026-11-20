using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
    public class ClinicVisitConfiguration : IEntityTypeConfiguration<ClinicVisit>
    {
        public void Configure(EntityTypeBuilder<ClinicVisit> builder)
        {
            builder.ToTable("ClinicVisits");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.DoctorName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Complaint).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.Temperature).HasPrecision(4, 1);
            builder.Property(x => x.Weight).HasPrecision(5, 2);
            builder.Property(x => x.Height).HasPrecision(5, 2);
            builder.Property(x => x.Diagnosis).HasMaxLength(2000);
            builder.Property(x => x.Treatment).HasMaxLength(2000);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.HasIndex(x => x.StudentId);
            builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(x => x.Medications).WithOne(m => m.Visit).HasForeignKey(m => m.VisitId).OnDelete(DeleteBehavior.Cascade);
            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
