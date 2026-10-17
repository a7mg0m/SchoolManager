using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class TimetableSlotConfiguration : IEntityTypeConfiguration<TimetableSlot>
	{
		public void Configure(EntityTypeBuilder<TimetableSlot> builder)
		{
            builder.ToTable("TimetableSlots");
            builder.HasKey(x => x.Id);

            builder.ConfigureLocalized(x => x.Name, "Name");
            builder.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Grade).WithMany().HasForeignKey(x => x.GradeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            builder.ConfigureAudit(x => x.AuditInfo);
		}
	}
}
