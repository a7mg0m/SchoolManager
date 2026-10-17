using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks
{
    public sealed class AuditInfo
    {
        public DateTime CreatedOn { get; }
        public Guid CreatedBy { get; }
        public DateTime? UpdatedOn { get; }
        public Guid? UpdatedBy { get; }

        public AuditInfo(DateTime createdOn, Guid createdBy,
                         DateTime? updatedOn = null, Guid? updatedBy = null)
        {
            CreatedOn = createdOn;
            CreatedBy = createdBy;
            UpdatedOn = updatedOn;
            UpdatedBy = updatedBy;
        }
    }
}
