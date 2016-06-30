using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    public struct MemoryPoolSpan
    {
        public MemoryPoolIterator Begin { get; private set; }
        public MemoryPoolIterator End { get; private set; }

        public MemoryPoolSpan(MemoryPoolIterator begin, MemoryPoolIterator end)
        {
            Begin = begin;
            End = end;
        }
    }
}
