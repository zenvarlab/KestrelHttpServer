// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public static class SocketInputExtensions
    {
        public static ValueTask<int> ReadAsync(this SocketInput input, byte[] buffer, int offset, int count)
        {
            while (input.IsCompleted)
            {
                var fin = input.CheckFinOrThrow();

                var begin = input.ConsumingStart();
                int actual;
                var end = begin.CopyTo(buffer, offset, count, out actual);
                input.ConsumingComplete(end, end);

                if (actual != 0 || fin)
                {
                    return new ValueTask<int>(actual);
                }
            }

            return new ValueTask<int>(input.ReadAsyncAwaited(buffer, offset, count));
        }

        private static async Task<int> ReadAsyncAwaited(this SocketInput input, byte[] buffer, int offset, int count)
        {
            while (true)
            {
                await input;

                var fin = input.CheckFinOrThrow();

                var begin = input.ConsumingStart();
                int actual;
                var end = begin.CopyTo(buffer, offset, count, out actual);
                input.ConsumingComplete(end, end);

                if (actual != 0 || fin)
                {
                    return actual;
                }
            }
        }

        public static bool Contains(ref ReadableBuffer buffer, byte b)
        {
            ReadCursor cursor;
            ReadableBuffer newBuffer;
            return buffer.TrySliceTo(b, out newBuffer, out cursor);
        }

        public static ValueTask<ArraySegment<byte>> PeekAsync(this IReadableChannel channel)
        {
            var input = channel.ReadAsync();
            while (input.IsCompleted)
            {
                var result = input.GetResult();

                var segment = result.Buffer.First;
                var x = result.Buffer.Slice(0, segment.Length);
                channel.Advance(x.End);

                ArraySegment<byte> data;
                Debug.Assert(segment.TryGetArray(out data));

                return new ValueTask<ArraySegment<byte>>(data);
            }

            return new ValueTask<ArraySegment<byte>>(channel.PeekAsyncAwaited());
        }

        private static async Task<ArraySegment<byte>> PeekAsyncAwaited(this IReadableChannel channel)
        {
            while (true)
            {
                var result = await channel.ReadAsync();

                var segment = result.Buffer.First;
                var x = result.Buffer.Slice(0, segment.Length);
                channel.Advance(x.End);


                if (segment.Length != 0 || result.IsCompleted)
                {
                    ArraySegment<byte> data;
                    Debug.Assert(segment.TryGetArray(out data));
                    return data;
                }
            }
        }
    }
}
