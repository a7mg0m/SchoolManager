using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class TimetableConfiguration : IEntityTypeConfiguration<Timetable>
	{
		public void Configure(EntityTypeBuilder<Timetable> builder)
		{
            builder.ToTable("Timetables");
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Grade).WithMany().HasForeignKey(x => x.GradeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.AcademicYear).WithMany().HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
            builder.HasMany(x => x.Slots).WithOne().HasForeignKey("TimetableId").OnDelete(DeleteBehavior.Cascade);
            builder.ConfigureAudit(x => x.AuditInfo);
		}
	}
}
