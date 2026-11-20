using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
    public class VaccinationCampaignConfiguration : IEntityTypeConfiguration<VaccinationCampaign>
    {
        public void Configure(EntityTypeBuilder<VaccinationCampaign> builder)
        {
            builder.ToTable("VaccinationCampaigns");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.HasOne(x => x.Vaccine).WithMany().HasForeignKey(x => x.VaccineId).OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAudit(x => x.AuditInfo);
        }
    }
}
