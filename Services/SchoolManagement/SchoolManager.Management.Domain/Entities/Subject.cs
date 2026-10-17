using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SchoolManager.Management.Domain
{
    public sealed class Subject : BaseEntity
    {
        public NameLocalized Name { get; private set; }
        public Guid GradeId { get; private set; }
        public bool IsActive { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        // Navigation
        public Grade? Grade { get; private set; }
        public List<SubjectTeacher> SubjectTeachers { get; private set; } = new();

        private Subject() { }

        public Subject(
            NameLocalized name,
            Guid gradeId,
            IEnumerable<Guid> teacherIds,
            AuditInfo auditInfo)
        {
            Name = name;
            GradeId = gradeId;
            IsActive = true;
            SetTeachers(teacherIds);

            AuditInfo = auditInfo;
        }

        public void Update(NameLocalized name, Guid gradeId, IEnumerable<Guid> teacherIds, AuditInfo auditInfo)
        {
            Name = name;
            GradeId = gradeId;
            SetTeachers(teacherIds);

            AuditInfo = auditInfo;
        }

        public void Deactivate(AuditInfo auditInfo)
        {
            IsActive = false;

            AuditInfo = auditInfo;
        }

        private void SetTeachers(IEnumerable<Guid> teacherIds)
        {
            var ids = teacherIds.Distinct().ToHashSet();

            SubjectTeachers.RemoveAll(x => !ids.Contains(x.TeacherId));

            var existing = SubjectTeachers.Select(x => x.TeacherId).ToHashSet();
            foreach (var id in ids.Where(id => !existing.Contains(id)))
                SubjectTeachers.Add(new SubjectTeacher(Id, id));
        }
    }
}
