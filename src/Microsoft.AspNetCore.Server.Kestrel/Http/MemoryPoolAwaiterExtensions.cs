// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public static class MemoryPoolAwaiterExtensions
    {
        public static ValueTask<int> ReadAsync(this MemoryPoolAwaiter input, byte[] buffer, int offset, int count)
        {
            while (input.IsCompleted)
            {
                var fin = input.RemoteIntakeFin;

                var begin = input.BeginRead();
                int actual;
                var end = begin.CopyTo(buffer, offset, count, out actual);
                input.EndRead(end, end);

                if (actual != 0)
                {
                    return new ValueTask<int>(actual);
                }
                else if (fin)
                {
                    return new ValueTask<int>(0);
                }
            }

            return new ValueTask<int>(input.ReadAsyncAwaited(buffer, offset, count));
        }

        private static async Task<int> ReadAsyncAwaited(this MemoryPoolAwaiter input, byte[] buffer, int offset, int count)
        {
            while (true)
            {
                await input;

                var fin = input.RemoteIntakeFin;

                var begin = input.BeginRead();
                int actual;
                var end = begin.CopyTo(buffer, offset, count, out actual);
                input.EndRead(end, end);

                if (actual != 0)
                {
                    return actual;
                }
                else if (fin)
                {
                    return 0;
                }
            }
        }
    }
}
