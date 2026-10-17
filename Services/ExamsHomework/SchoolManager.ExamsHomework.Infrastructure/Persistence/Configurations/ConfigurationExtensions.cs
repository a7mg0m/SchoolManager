using BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;

namespace SchoolManager.ExamsHomework.Infrastructure
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
    }
}
