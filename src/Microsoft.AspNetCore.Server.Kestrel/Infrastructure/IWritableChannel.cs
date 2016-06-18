using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    public interface IWritableChannel
    {
        MemoryPoolIterator BeginWrite();
        Task EndWriteAsync(MemoryPoolIterator end);

        void CompleteWriting(Exception error = null);

        void Close();
    }
}
