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

        /// <summary>
        /// A new instance with the same values. AuditInfo is mapped as an EF Core owned type, and owned
        /// instances are tracked by reference: one instance shared by several entities is saved for only
        /// one of them (the others get NULL audit columns). Entities that may receive a shared instance
        /// (e.g. children created in a loop) store a copy.
        /// </summary>
        public AuditInfo Copy() => new(CreatedOn, CreatedBy, UpdatedOn, UpdatedBy);

        /// <summary>Same creation values, with UpdatedOn/UpdatedBy set.</summary>
        public AuditInfo Updated(DateTime updatedOn, Guid? updatedBy) => new(CreatedOn, CreatedBy, updatedOn, updatedBy);
    }
}
