using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.ExamsHomework.Domain
{
    public interface IHomeworkRepository : IRepository<Homework>
    {
        /// <summary>A teacher's homework with its questions, newest due date first</summary>
        Task<List<Homework>> GetForTeacherAsync(Guid teacherId, CancellationToken cancellationToken = default);

        /// <summary>One homework with its questions (tracked, for changes)</summary>
        Task<Homework?> GetWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Homework published to a class, with the questions and the publish date</summary>
        Task<List<(Homework Homework, DateTime PublishedOn)>> GetPublishedForSectionAsync(Guid academicYearId, Guid sectionId, CancellationToken cancellationToken = default);
    }
   
}
