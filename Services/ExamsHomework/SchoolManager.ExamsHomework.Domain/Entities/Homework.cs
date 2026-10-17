using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace SchoolManager.ExamsHomework.Domain
{
    public sealed class Homework : BaseEntity
    {
        public Guid SubjectId { get; private set; }
        public Guid TeacherId { get; private set; }
        public Guid GradeId { get; private set; }
        public Guid AcademicYearId { get; private set; }
        public Guid SemesterId { get; private set; }

        public string Title { get; private set; }
        public string Description { get; private set; }
        public DateOnly DueDate { get; private set; }

        public string? AttachmentUrl { get; private set; }   // صورة الواجب

        public AuditInfo AuditInfo { get; private set; }
        public List<HomeworkQuestion> HomeworkQuestions { get; private set; } = new();


        private Homework() { }

        public Homework(
            Guid subjectId,
            Guid teacherId,
            Guid gradeId,
            Guid academicYearId,
            Guid semesterId,
            string title,
            string description,
            DateOnly dueDate,
            string? attachmentUrl,
            AuditInfo auditInfo)
        {
            SubjectId = subjectId;
            TeacherId = teacherId;
            GradeId = gradeId;
            AcademicYearId = academicYearId;
            SemesterId = semesterId;

            Title = title;
            Description = description;
            DueDate = dueDate;
            AttachmentUrl = attachmentUrl;

            AuditInfo = auditInfo;
        }

        public void Update(string title, string description, DateOnly dueDate, string? attachmentUrl, AuditInfo auditInfo)
        {
            Title = title;
            Description = description;
            DueDate = dueDate;
            AttachmentUrl = attachmentUrl;
            AuditInfo = auditInfo;
        }
        public void AddQuestion(Guid homeworkId,
            string questionHeader,
            List<string>? questions,
            string? imageUrl,
            AuditInfo auditInfo)
        {
            HomeworkQuestions.Add(new HomeworkQuestion(homeworkId, questionHeader, questions, imageUrl, auditInfo));

        }
    }
}
