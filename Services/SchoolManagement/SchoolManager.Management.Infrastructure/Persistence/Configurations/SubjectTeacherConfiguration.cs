using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManager.Management.Domain;

namespace SchoolManager.Management.Infrastructure
{
    public class SubjectTeacherConfiguration : IEntityTypeConfiguration<SubjectTeacher>
    {
        public void Configure(EntityTypeBuilder<SubjectTeacher> builder)
        {
            builder.ToTable("SubjectTeachers");
            builder.HasKey(x => new { x.SubjectId, x.TeacherId });
            builder.HasOne(x => x.Subject).WithMany(s => s.SubjectTeachers).HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Teacher).WithMany(t => t.SubjectTeachers).HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
