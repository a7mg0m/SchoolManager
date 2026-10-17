using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.ExamsHomework.Domain
{
    public sealed class HomeworkQuestion : BaseEntity
    {
        public Guid HomeworkId { get; private set; }
        public string QuestionHeader { get; private set; }

        public List<string>? Questions { get; private set; }

        public string? ImageUrl { get; private set; }   // صورة السؤال

        public AuditInfo AuditInfo { get; private set; }

        private HomeworkQuestion() { }

        public HomeworkQuestion(
            Guid homeworkId,
            string questionHeader,
            List<string>? questions,
            string? imageUrl,
            AuditInfo auditInfo)
        {
            HomeworkId = homeworkId;
            QuestionHeader = questionHeader;
            Questions = questions;
            ImageUrl = imageUrl;

            AuditInfo = auditInfo;
        }
        public void Update(
            string questionHeader,
            List<string>? questions,
            string? imageUrl,
            AuditInfo auditInfo)
        {
            QuestionHeader = questionHeader;
            Questions = questions;
            ImageUrl = imageUrl;
            AuditInfo = auditInfo;
        }
    }

}
