using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks
{
    public sealed class NameLocalized
    {
        public string Arabic { get; }
        public string English { get; }

        public NameLocalized(string arabic, string english)
        {
            Arabic = arabic;
            English = english;
        }
    }
}
