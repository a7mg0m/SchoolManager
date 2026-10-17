using System;

namespace SchoolManager.Management.Domain
{
    public sealed class SubjectTeacher
    {
        public Guid SubjectId { get; private set; }
        public Guid TeacherId { get; private set; }

        // Navigation
        public Subject? Subject { get; private set; }
        public Teacher? Teacher { get; private set; }

        private SubjectTeacher() { }

        public SubjectTeacher(Guid subjectId, Guid teacherId)
        {
            SubjectId = subjectId;
            TeacherId = teacherId;
        }
    }
}
