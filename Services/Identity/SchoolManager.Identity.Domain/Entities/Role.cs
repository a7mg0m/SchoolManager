using BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Identity.Domain
{
    public class Role : BaseEntity
    {
        public NameLocalized Name { get; private set; }
        public string Description { get; private set; }


        public Role()
        {
        }
        public Role(NameLocalized name, string description)
        {
            Name = name;
            Description = description;
        }
        public void Update(NameLocalized name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
