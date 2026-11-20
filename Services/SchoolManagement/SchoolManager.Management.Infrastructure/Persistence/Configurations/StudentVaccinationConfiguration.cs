using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
    public class StudentVaccinationConfiguration : IEntityTypeConfiguration<StudentVaccination>
    {
        public void Configure(EntityTypeBuilder<StudentVaccination> builder)
        {
            builder.ToTable("StudentVaccinations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Notes).HasMaxLength(1000);
            builder.HasIndex(x => new { x.StudentId, x.VaccineId, x.DoseNumber }).IsUnique();
            builder.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Vaccine).WithMany().HasForeignKey(x => x.VaccineId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Campaign).WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
