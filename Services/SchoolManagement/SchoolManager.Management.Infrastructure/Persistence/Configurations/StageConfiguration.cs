using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class StageConfiguration : IEntityTypeConfiguration<Stage>
	{
		public void Configure(EntityTypeBuilder<Stage> builder)
		{
            builder.ToTable("Stages");
            builder.HasKey(x => x.Id);

            builder.ConfigureLocalized(x => x.Name, "Name");
            builder.HasMany(x => x.Grades).WithOne(g => g.Stage).HasForeignKey(g => g.StageId).OnDelete(DeleteBehavior.Restrict);
			builder.ConfigureAudit(x => x.AuditInfo);

			var seed = new[]
			{
				(Id: Guid.Parse("11111111-1111-1111-1111-000000000001"), Ar: "مرحلة الحضانة", En: "Nursery Stage"),
				(Id: Guid.Parse("11111111-1111-1111-1111-000000000002"), Ar: "المرحلة الابتدائية", En: "Primary Stage"),
				(Id: Guid.Parse("11111111-1111-1111-1111-000000000003"), Ar: "المرحلة الإعدادية", En: "Preparatory Stage"),
				(Id: Guid.Parse("11111111-1111-1111-1111-000000000004"), Ar: "المرحلة الثانوية", En: "Secondary Stage"),
			};
			var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

			builder.HasData(seed.Select(s => new { s.Id, IsActive = true }));
			builder.OwnsOne(x => x.Name).HasData(seed.Select(s => new { StageId = s.Id, Arabic = s.Ar, English = s.En }));
			builder.OwnsOne(x => x.AuditInfo).HasData(seed.Select(s => new { StageId = s.Id, CreatedOn = seedDate, CreatedBy = Guid.Empty }));
		}
	}
}
