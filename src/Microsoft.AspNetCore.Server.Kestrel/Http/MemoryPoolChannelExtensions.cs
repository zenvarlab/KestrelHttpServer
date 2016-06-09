// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public static class MemoryPoolChannelExtensions
    {
        public static async Task CopyToAsync(this MemoryPoolChannel input, MemoryPoolChannel output)
        {
            while (true)
            {
                await input;

                var fin = input.Completed;

                var begin = input.BeginRead();
                var end = input.End();

                try
                {
                    var block = begin.Block;

                    var writeEnd = output.BeginWrite();

                    while (true)
                    {
                        var blockStart = block == begin.Block ? begin.Index : block.Data.Offset;
                        var blockEnd = block == end.Block ? end.Index : block.Data.Offset + block.Data.Count;

                        writeEnd.CopyFrom(block.Array, blockStart, blockEnd - blockStart);

                        if (block == end.Block)
                        {
                            break;
                        }

                        block = block.Next;
                    }

                    await output.EndWrite(writeEnd);
                }
                finally
                {
                    input.EndRead(end);
                }

                if (fin)
                {
                    return;
                }
            }
        }

        public static ValueTask<int> ReadAsync(this MemoryPoolChannel input, byte[] buffer, int offset, int count)
        {
            while (input.IsCompleted)
            {
                var fin = input.Completed;

                var begin = input.BeginRead();
                int actual;
                var end = begin.CopyTo(buffer, offset, count, out actual);
                input.EndRead(end);

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

        private static async Task<int> ReadAsyncAwaited(this MemoryPoolChannel input, byte[] buffer, int offset, int count)
        {
            while (true)
            {
                await input;

                var fin = input.Completed;

                var begin = input.BeginRead();
                int actual;
                var end = begin.CopyTo(buffer, offset, count, out actual);
                input.EndRead(end);

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
