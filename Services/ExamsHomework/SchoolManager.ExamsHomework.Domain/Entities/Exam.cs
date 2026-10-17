using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.ExamsHomework.Domain
{
    public sealed class Exam : BaseEntity
    {
        public Guid SubjectId { get; private set; }
        public Guid GradeId { get; private set; }
        public Guid SectionId { get; private set; }
        public Guid AcademicYearId { get; private set; }
        public Guid SemesterId { get; private set; }

        public ExamType ExamType { get; private set; }
        public DateOnly ExamDate { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public TimeOnly EndTime { get; private set; }
        public double Duration { get; private set; }

        public string? AnswerModelUrl { get; private set; } // نموذج الإجابة (ملف)

        public AuditInfo AuditInfo { get; private set; }
        public List<ExamResult> ExamResults { get; private set; } = new();



        private Exam() { }

        public Exam(
            Guid subjectId,
            Guid gradeId,
            Guid sectionId,
            Guid academicYearId,
            Guid semesterId,
            ExamType examType,
            DateOnly examDate,
            TimeOnly startTime,
            TimeOnly endTime,
            double duration,
            string? answerModelUrl,
            AuditInfo auditInfo)
        {
            StartTime = startTime;
            EndTime = endTime;
            Duration = duration;
            SubjectId = subjectId;
            GradeId = gradeId;
            SectionId = sectionId;
            AcademicYearId = academicYearId;
            SemesterId = semesterId;
            ExamType = examType;
            ExamDate = examDate;
            StartTime = startTime;
            EndTime = endTime;
            Duration = duration;
            AnswerModelUrl = answerModelUrl;

            AuditInfo = auditInfo;
        }

        public void Update(Guid subjectId,
            Guid gradeId,
            Guid sectionId,
            Guid academicYearId,
            Guid semesterId,
            ExamType examType,
            DateOnly examDate,
            TimeOnly startTime,
            TimeOnly endTime,
            double duration,
            string? answerModelUrl  , AuditInfo auditInfo)
        {
            StartTime = startTime;
            EndTime = endTime;
            Duration = duration;
            GradeId = gradeId;
            SectionId = sectionId;
            SubjectId = subjectId;
            AcademicYearId = academicYearId;
            SemesterId = semesterId;
            ExamType = examType;
            ExamDate = examDate;
            AnswerModelUrl = answerModelUrl;
            AuditInfo = auditInfo;
        }
    }

}
