using BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;

namespace SchoolManager.Management.Infrastructure
{
	public static class ConfigurationExtensions
	{
		public static void ConfigureAudit<T>(this EntityTypeBuilder<T> builder, Expression<Func<T, AuditInfo?>> navigation) where T : class
		{
			builder.OwnsOne(navigation, audit =>
			{
				audit.Property(a => a.CreatedOn).HasColumnName("CreatedOn");
				audit.Property(a => a.CreatedBy).HasColumnName("CreatedBy");
				audit.Property(a => a.UpdatedOn).HasColumnName("UpdatedOn");
				audit.Property(a => a.UpdatedBy).HasColumnName("UpdatedBy");
			});
		}

		public static void ConfigureLocalized<T>(this EntityTypeBuilder<T> builder, Expression<Func<T, NameLocalized?>> navigation, string columnPrefix) where T : class
		{
			builder.OwnsOne(navigation, name =>
			{
				name.Property(n => n.Arabic).HasColumnName(columnPrefix + "Ar").IsRequired().HasMaxLength(200);
				name.Property(n => n.English).HasColumnName(columnPrefix + "En").IsRequired().HasMaxLength(200);
			});
		}
	}
}
