using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.ExamsHomework.Domain
{
    public interface IHomeworkPublishRepository : IRepository<HomeworkPublish>
    {
        /// <summary>Publishes of these homeworks</summary>
        Task<List<HomeworkPublish>> GetForHomeworksAsync(IReadOnlyCollection<Guid> homeworkIds, CancellationToken cancellationToken = default);
    }
   
}
