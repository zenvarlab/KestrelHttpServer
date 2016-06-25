using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    // TODO: Make this usable without the awaitable, split that into a struct
    public interface IReadableChannel : ICriticalNotifyCompletion
    {
        // Make it awaitable
        bool IsCompleted { get; }
        void GetResult();
        IReadableChannel GetAwaiter();

        bool Completed { get; }

        MemoryPoolSpan BeginRead();
        void EndRead(MemoryPoolIterator consumed, MemoryPoolIterator examined);

        void CompleteReading();
    }

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
