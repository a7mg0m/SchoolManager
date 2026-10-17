using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.ExamsHomework.Domain
{
    public sealed class ExamResult : BaseEntity
    {
        public Guid ExamId { get; private set; }
        public Guid StudentId { get; private set; }

        public double Score { get; private set; }

        public AuditInfo AuditInfo { get; private set; }

        private ExamResult() { }

        public ExamResult(Guid examId, Guid studentId, int score, AuditInfo auditInfo)
        {
            ExamId = examId;
            StudentId = studentId;
            Score = score;

            AuditInfo = auditInfo;
        }

        public void UpdateScore(Guid studentId, double score, AuditInfo auditInfo)
        {
            StudentId = studentId;
            Score = score;

            AuditInfo = auditInfo;
        }
    }

}
