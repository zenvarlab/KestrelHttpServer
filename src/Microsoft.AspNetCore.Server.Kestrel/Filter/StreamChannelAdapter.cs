// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Filter
{
    public class StreamChannelAdapter
    {
        private readonly Stream _stream;

        public StreamChannelAdapter(Stream stream, MemoryPool memory, IThreadPool threadPool)
        {
            InputChannel = new MemoryPoolChannel(memory, threadPool);
            OutputChannel = new MemoryPoolChannel(memory, threadPool);
            _stream = stream;
        }

        public MemoryPoolChannel InputChannel { get; private set; }

        public MemoryPoolChannel OutputChannel { get; private set; }

        public async void Start()
        {
            var reads = ReadFromStream();
            var writes = WriteToStream();

            await writes;

            _stream.Dispose();

            OutputChannel.CompleteWriting();
        }

        private async Task ReadFromStream()
        {
            try
            {
                while (true)
                {
                    var end = OutputChannel.BeginWrite();
                    var block = end.Block;

                    try
                    {
                        int bytesRead = await _stream.ReadAsync(block.Array, block.End, block.Data.Offset + block.Data.Count - block.End);

                        if (bytesRead == 0)
                        {
                            OutputChannel.CompleteWriting();
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
                        OutputChannel.CompleteWriting(error);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                OutputChannel.CompleteWriting(ex);
            }
            finally
            {

            }
        }

        private async Task WriteToStream()
        {
            try
            {
                while (!InputChannel.Completed)
                {
                    await InputChannel;

                    if (InputChannel.Completed)
                    {
                        break;
                    }

                    var span = InputChannel.BeginRead();
                    var start = span.Begin;
                    var end = span.End;

                    try
                    {
                        if (end.IsDefault)
                        {
                            continue;
                        }

                        await start.CopyToAsync(_stream, end.Block);
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

            InputChannel.CompleteReading();
        }
    }
}
