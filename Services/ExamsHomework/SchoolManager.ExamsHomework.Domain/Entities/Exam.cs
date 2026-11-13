using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.ExamsHomework.Domain
{
    public sealed class Exam : BaseEntity
    {
        public Guid SubjectId { get; private set; }
        public Guid StageId { get; private set; }
        public Guid GradeId { get; private set; }
        public Guid? SectionId { get; private set; } // Optional: null = exam for the whole grade
        public Guid AcademicYearId { get; private set; }
        public Guid SemesterId { get; private set; }

        public ExamType ExamType { get; private set; }
        public DateOnly ExamDate { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public TimeOnly EndTime { get; private set; }
        public double Duration { get; private set; }

        /// <summary>Full mark of the exam (maximum score a student can get)</summary>
        public double Weight { get; private set; }
        /// <summary>Minimum score to pass (0..Weight)</summary>
        public double SuccessScore { get; private set; }

        public string? AnswerModelUrl { get; private set; } // نموذج الإجابة (ملف)

        public AuditInfo AuditInfo { get; private set; }
        public List<ExamResult> ExamResults { get; private set; } = new();



        private Exam() { }

        public Exam(
            Guid subjectId,
            Guid stageId,
            Guid gradeId,
            Guid? sectionId,
            Guid academicYearId,
            Guid semesterId,
            ExamType examType,
            DateOnly examDate,
            TimeOnly startTime,
            TimeOnly endTime,
            double duration,
            double weight,
            double successScore,
            string? answerModelUrl,
            AuditInfo auditInfo)
        {
            Weight = weight;
            SuccessScore = successScore;
            SubjectId = subjectId;
            StageId = stageId;
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
            Guid stageId,
            Guid gradeId,
            Guid? sectionId,
            Guid academicYearId,
            Guid semesterId,
            ExamType examType,
            DateOnly examDate,
            TimeOnly startTime,
            TimeOnly endTime,
            double duration,
            double weight,
            double successScore,
            string? answerModelUrl, AuditInfo auditInfo)
        {
            Weight = weight;
            SuccessScore = successScore;
            SubjectId = subjectId;
            StageId = stageId;
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

        /// <summary>A score passes when it reaches the success score</summary>
        public bool IsPassed(double score) => score >= SuccessScore;
    }

}
