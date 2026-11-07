using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
	public class GradeConfiguration : IEntityTypeConfiguration<Grade>
	{
		public void Configure(EntityTypeBuilder<Grade> builder)
		{
            builder.ToTable("Grades");
            builder.HasKey(x => x.Id);

            builder.ConfigureLocalized(x => x.Name, "Name");
            builder.Ignore(x => x.AcademicYear);
            builder.HasMany(x => x.Sections).WithOne(s => s.Grade).HasForeignKey(s => s.GradeId).OnDelete(DeleteBehavior.Restrict);
			builder.ConfigureAudit(x => x.AuditInfo);

			var nurseryId = Guid.Parse("11111111-1111-1111-1111-000000000001");
			var primaryId = Guid.Parse("11111111-1111-1111-1111-000000000002");
			var preparatoryId = Guid.Parse("11111111-1111-1111-1111-000000000003");
			var secondaryId = Guid.Parse("11111111-1111-1111-1111-000000000004");

			var seed = new List<(Guid StageId, string Ar, string En)>
			{
				(nurseryId, "KG 1", "KG 1"),
				(nurseryId, "KG 2", "KG 2"),
				(primaryId, "الصف الأول", "Grade 1"),
				(primaryId, "الصف الثاني", "Grade 2"),
				(primaryId, "الصف الثالث", "Grade 3"),
				(primaryId, "الصف الرابع", "Grade 4"),
				(primaryId, "الصف الخامس", "Grade 5"),
				(primaryId, "الصف السادس", "Grade 6"),
				(preparatoryId, "الصف الأول", "Grade 1"),
				(preparatoryId, "الصف الثاني", "Grade 2"),
				(preparatoryId, "الصف الثالث", "Grade 3"),
				(secondaryId, "الصف الأول", "Grade 1"),
				(secondaryId, "الصف الثاني", "Grade 2"),
				(secondaryId, "الصف الثالث", "Grade 3"),
			};
			var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var rows = seed.Select((s, i) => new
			{
				Id = Guid.Parse($"22222222-2222-2222-2222-{i + 1:D12}"),
				s.StageId,
				s.Ar,
				s.En
			}).ToList();

			builder.HasData(rows.Select(r => new { r.Id, r.StageId, IsActive = true }));
			builder.OwnsOne(x => x.Name).HasData(rows.Select(r => new { GradeId = r.Id, Arabic = r.Ar, English = r.En }));
			builder.OwnsOne(x => x.AuditInfo).HasData(rows.Select(r => new { GradeId = r.Id, CreatedOn = seedDate, CreatedBy = Guid.Empty }));
		}
	}
}
