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
        MemoryPoolIterator BeginRead();
        void EndRead(MemoryPoolIterator consumed, MemoryPoolIterator examined);
        MemoryPoolIterator End();

        void Close();
    }
}
