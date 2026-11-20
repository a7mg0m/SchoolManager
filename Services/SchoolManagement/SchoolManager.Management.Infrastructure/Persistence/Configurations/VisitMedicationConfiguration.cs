using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
    public class VisitMedicationConfiguration : IEntityTypeConfiguration<VisitMedication>
    {
        public void Configure(EntityTypeBuilder<VisitMedication> builder)
        {
            builder.ToTable("VisitMedications");
            builder.HasKey(x => x.Id);
            // Ids come from the entity: medications added to a visit's list are inserted (not taken as existing rows)
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.MedicineName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Dose).IsRequired().HasMaxLength(200);
        }
    }
}
