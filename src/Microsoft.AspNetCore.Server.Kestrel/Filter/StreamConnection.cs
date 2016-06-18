// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Filter
{
    public class StreamConnection : IDisposable
    {
        private readonly string _connectionId;
        private readonly Stream _stream;
        private readonly IKestrelTrace _log;

        public StreamConnection(
            string connectionId,
            Stream stream,
            MemoryPool memory,
            IKestrelTrace logger,
            IThreadPool threadPool)
        {
            InputChannel = new MemoryPoolChannel(memory, threadPool);
            OutputChannel = new MemoryPoolChannel(memory, threadPool);

            _connectionId = connectionId;
            _log = logger;
            _stream = stream;
        }

        public MemoryPoolChannel InputChannel { get; private set; }

        public MemoryPoolChannel OutputChannel { get; private set; }

        public void Start()
        {
            var reads = ReadFromStream();
            var writes = WriteToStream();
        }

        private async Task ReadFromStream()
        {
            try
            {
                // Read input from the stream
                while (true)
                {
                    var end = OutputChannel.BeginWrite();
                    var block = end.Block;

                    try
                    {
                        int bytesRead = await _stream.ReadAsync(block.Array, block.End, block.Data.Offset + block.Data.Count - block.End);

                        if (bytesRead == 0)
                        {
                            break;
                        }
                        else
                        {
                            end.UpdateEnd(bytesRead);
                            await OutputChannel.EndWriteAsync(end);
                        }
                    }
                    catch (Exception error)
                    {
                        // await OutputChannel.EndWriteAsync(end, error);
                    }
                }
            }
            catch (Exception)
            {
                OutputChannel.Cancel();
                // _log.LogError(0, copyAsyncTask.Exception, "FilteredStreamAdapter.CopyToAsync");
            }
            finally
            {
                // OutputChannel.IncomingFin();
            }
        }

        private async Task WriteToStream()
        {
            try
            {
                while (!InputChannel.Completed)
                {
                    await InputChannel;

                    var start = InputChannel.BeginRead();
                    var end = InputChannel.End();

                    try
                    {
                        var block = start.Block;

                        while (true)
                        {
                            var blockStart = block == start.Block ? start.Index : block.Data.Offset;
                            var blockEnd = block == end.Block ? end.Index : block.Data.Offset + block.Data.Count;
                            var length = blockEnd - blockStart;

                            await _stream.WriteAsync(block.Array, blockStart, length);

                            if (block == end.Block)
                            {
                                break;
                            }

                            block = block.Next;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle
                        break;
                    }
                    finally
                    {
                        InputChannel.EndRead(end);
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: Log
            }
        }

        public void Dispose()
        {
            InputChannel.Dispose();
            OutputChannel.Dispose();
        }
    }
}
