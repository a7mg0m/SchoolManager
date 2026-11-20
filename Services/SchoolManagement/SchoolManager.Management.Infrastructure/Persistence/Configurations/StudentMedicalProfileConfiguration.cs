using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
    public class StudentMedicalProfileConfiguration : IEntityTypeConfiguration<StudentMedicalProfile>
    {
        public void Configure(EntityTypeBuilder<StudentMedicalProfile> builder)
        {
            builder.ToTable("StudentMedicalProfiles");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BloodType).HasMaxLength(10);
            builder.Property(x => x.Allergies).HasMaxLength(1000);
            builder.Property(x => x.ChronicDiseases).HasMaxLength(1000);
            builder.Property(x => x.RegularMedications).HasMaxLength(1000);
            builder.Property(x => x.EmergencyPhone).HasMaxLength(50);
            builder.Property(x => x.Notes).HasMaxLength(2000);
            builder.HasIndex(x => x.StudentId).IsUnique();
            builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
