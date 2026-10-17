using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; }

        protected BaseEntity()
        {
            Id = Guid.NewGuid();
        }
    }
}
